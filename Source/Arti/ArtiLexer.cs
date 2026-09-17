using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiLexResult
    {
        public ArtiLexResult()
        {
            Tokens = new List<ArtiToken>();
            Diagnostics = new List<ArtiDiagnostic>();
        }

        public IList<ArtiToken> Tokens { get; }
        public IList<ArtiDiagnostic> Diagnostics { get; }
    }

    public sealed class ArtiLexer : ArtiDiagnosticReporter
    {
        private static readonly IDictionary<string, ArtiTokenKind> Keywords = new Dictionary<string, ArtiTokenKind>(StringComparer.Ordinal)
        {
            { "true", ArtiTokenKind.True },
            { "false", ArtiTokenKind.False },
            { "null", ArtiTokenKind.Null },
            { "use", ArtiTokenKind.Use },
            { "optional", ArtiTokenKind.Optional },
            { "as", ArtiTokenKind.As },
            { "group", ArtiTokenKind.Group },
            { "const", ArtiTokenKind.Const },
            { "let", ArtiTokenKind.Let },
            { "fn", ArtiTokenKind.Fn },
            { "if", ArtiTokenKind.If },
            { "else", ArtiTokenKind.Else },
            { "for", ArtiTokenKind.For },
            { "in", ArtiTokenKind.In },
            { "while", ArtiTokenKind.While },
            { "return", ArtiTokenKind.Return },
            { "break", ArtiTokenKind.Break },
            { "continue", ArtiTokenKind.Continue }
        };

        private string _source;
        private int _position;
        private int _line;
        private int _column;
        private int _sourceOffset;
        private ArtiLexResult _result;

        public ArtiLexResult Lex(string source)
        {
            return Lex(source, 0, 1, 1);
        }

        public ArtiLexResult Lex(string source, int sourceOffset, int startLine, int startColumn)
        {
            _source = source ?? string.Empty;
            _position = 0;
            _line = Math.Max(1, startLine);
            _column = Math.Max(1, startColumn);
            _sourceOffset = Math.Max(0, sourceOffset);
            _result = new ArtiLexResult();
            ClearDiagnostics();

            while (!End)
            {
                ReadNextToken();
            }

            _result.Tokens.Add(new ArtiToken(
                ArtiTokenKind.EndOfFile,
                string.Empty,
                null,
                CreateSpan(_position, 0, _line, _column)));
            foreach (ArtiDiagnostic diagnostic in ReportedDiagnostics)
            {
                _result.Diagnostics.Add(diagnostic);
            }

            return _result;
        }

        private void ReadNextToken()
        {
            char current = Current;
            if (current == '\\' && (Peek(1) == '\r' || Peek(1) == '\n'))
            {
                Advance();
                Advance();
                return;
            }
            if (current == ' ' || current == '\t' || current == '\f' || current == '\v')
            {
                Advance();
                return;
            }

            if (current == '\r' || current == '\n')
            {
                int start = _position;
                int line = _line;
                int column = _column;
                Advance();
                _result.Tokens.Add(new ArtiToken(
                    ArtiTokenKind.NewLine,
                    _source.Substring(start, _position - start),
                    null,
                    CreateSpan(start, _position - start, line, column)));
                return;
            }

            if (current == '/' && Peek(1) == '/')
            {
                SkipLineComment();
                return;
            }

            if (current == '/' && Peek(1) == '*')
            {
                ReportError(1001, CurrentSpan(2));
                Advance();
                Advance();
                while (!End && !(Current == '*' && Peek(1) == '/'))
                {
                    Advance();
                }

                if (!End)
                {
                    Advance();
                    Advance();
                }

                return;
            }

            if (current == 'f' && (Peek(1) == '"' || Peek(1) == '\''))
            {
                ReadInterpolatedString();
                return;
            }

            if (IsIdentifierStart(current))
            {
                ReadIdentifier();
                return;
            }

            if (char.IsDigit(current))
            {
                ReadNumber();
                return;
            }

            if (current == '"' || current == '\'')
            {
                ReadString(current);
                return;
            }

            ReadPunctuationOrOperator();
        }

        private void ReadIdentifier()
        {
            int start = _position;
            int line = _line;
            int column = _column;
            while (!End && IsIdentifierPart(Current))
            {
                Advance();
            }

            string text = _source.Substring(start, _position - start);
            ArtiTokenKind kind;
            if (!Keywords.TryGetValue(text, out kind))
            {
                kind = ArtiTokenKind.Identifier;
            }

            _result.Tokens.Add(new ArtiToken(
                kind,
                text,
                kind == ArtiTokenKind.Identifier ? text : null,
                CreateSpan(start, _position - start, line, column)));
        }

        private void ReadNumber()
        {
            int start = _position;
            int line = _line;
            int column = _column;
            bool isFloat = false;

            while (!End && char.IsDigit(Current))
            {
                Advance();
            }

            if (!End && Current == '.' && char.IsDigit(Peek(1)))
            {
                isFloat = true;
                Advance();
                while (!End && char.IsDigit(Current))
                {
                    Advance();
                }
            }

            if (!End && (Current == 'e' || Current == 'E'))
            {
                isFloat = true;
                Advance();
                if (!End && (Current == '+' || Current == '-'))
                {
                    Advance();
                }

                int exponentStart = _position;
                while (!End && char.IsDigit(Current))
                {
                    Advance();
                }

                if (exponentStart == _position)
                {
                    ReportError(1002, CreateSpan(start, _position - start, line, column));
                }
            }

            string text = _source.Substring(start, _position - start);
            object value;
            if (isFloat)
            {
                double parsedFloat;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedFloat))
                {
                    ReportError(1003, CreateSpan(start, _position - start, line, column));
                    parsedFloat = 0d;
                }

                value = parsedFloat;
            }
            else
            {
                int parsedInteger;
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedInteger))
                {
                    value = parsedInteger;
                }
                else
                {
                    ReportError(1004, CreateSpan(start, _position - start, line, column));
                    value = 0;
                }
            }

            _result.Tokens.Add(new ArtiToken(
                isFloat ? ArtiTokenKind.Float : ArtiTokenKind.Integer,
                text,
                value,
                CreateSpan(start, _position - start, line, column)));
        }

        private void ReadString(char quote)
        {
            int start = _position;
            int line = _line;
            int column = _column;
            StringBuilder value = new StringBuilder();
            int quoteLength = QuoteLength();
            for (int i = 0; i < quoteLength; i++) Advance();
            bool closed = false;

            while (!End)
            {
                char current = Current;
                if (AtClosingQuote(quote, quoteLength))
                {
                    for (int i = 0; i < quoteLength; i++) Advance();
                    closed = true;
                    break;
                }

                if (current != '\\')
                {
                    value.Append(current == '\r' ? '\n' : current);
                    Advance();
                    continue;
                }

                ReadEscape(value, start, line, column);
            }

            if (!closed)
            {
                ReportError(1006, CreateSpan(start, _position - start, line, column));
            }

            _result.Tokens.Add(new ArtiToken(
                ArtiTokenKind.String,
                _source.Substring(start, _position - start),
                value.ToString(),
                CreateSpan(start, _position - start, line, column)));
        }

        private void ReadEscape(StringBuilder value, int start, int line, int column)
        {
            Advance();
            if (End) return;
            char escaped = Current;
            Advance();
            switch (escaped)
            {
                case '\r': case '\n': break;
                case 'n': value.Append('\n'); break;
                case 'r': value.Append('\r'); break;
                case 't': value.Append('\t'); break;
                case '0': value.Append('\0'); break;
                case '\\': case '"': case '\'': value.Append(escaped); break;
                case 'u': value.Append(ReadUnicodeEscape(start, line, column)); break;
                default:
                    ReportError(1005, CurrentSpan(0), escaped);
                    value.Append(escaped);
                    break;
            }
        }

        internal static int SkipString(string source, int start)
        {
            var lexer = new ArtiLexer { _source = source, _position = start, _line = 1, _column = 1,
                _result = new ArtiLexResult() };
            if (lexer.Current == 'f') lexer.ReadInterpolatedString();
            else lexer.ReadString(lexer.Current);
            return lexer._position;
        }

        private void ReadInterpolatedString()
        {
            int start = _position, line = _line, column = _column;
            Advance();
            char quote = Current;
            int quoteLength = QuoteLength();
            for (int i = 0; i < quoteLength; i++) Advance();
            _result.Tokens.Add(new ArtiToken(ArtiTokenKind.InterpolatedStringStart,
                _source.Substring(start, 1 + quoteLength), null, CreateSpan(start, 1 + quoteLength, line, column)));
            while (!End && !AtClosingQuote(quote, quoteLength))
            {
                int textStart = _position, textLine = _line, textColumn = _column;
                var text = new StringBuilder();
                while (!End && !AtClosingQuote(quote, quoteLength))
                {
                    if ((Current == '{' || Current == '}') && Peek(1) == Current)
                    {
                        text.Append(Current); Advance(); Advance();
                    }
                    else if (Current == '{') break;
                    else if (Current == '}')
                    {
                        ReportError(1009, CurrentSpan(1)); text.Append(Current); Advance();
                    }
                    else if (Current == '\\') ReadEscape(text, textStart, textLine, textColumn);
                    else { text.Append(Current == '\r' ? '\n' : Current); Advance(); }
                }
                if (_position > textStart)
                    _result.Tokens.Add(new ArtiToken(ArtiTokenKind.String,
                        _source.Substring(textStart, _position - textStart), text.ToString(),
                        CreateSpan(textStart, _position - textStart, textLine, textColumn)));
                if (End || AtClosingQuote(quote, quoteLength)) break;

                _result.Tokens.Add(new ArtiToken(ArtiTokenKind.InterpolationStart, "{", null, CurrentSpan(1)));
                Advance();
                int depth = 0;
                while (!End)
                {
                    if (Current == '}' && depth == 0) break;
                    if (Current == '{') depth++;
                    else if (Current == '}') depth--;
                    ReadNextToken();
                }
                if (End) { ReportError(1009, CurrentSpan(0)); break; }
                _result.Tokens.Add(new ArtiToken(ArtiTokenKind.InterpolationEnd, "}", null, CurrentSpan(1)));
                Advance();
            }
            if (End) ReportError(1006, CreateSpan(start, _position - start, line, column));
            else
            {
                _result.Tokens.Add(new ArtiToken(ArtiTokenKind.InterpolatedStringEnd, new string(quote, quoteLength), null, CurrentSpan(quoteLength)));
                for (int i = 0; i < quoteLength; i++) Advance();
            }
        }

        private int QuoteLength() => Peek(1) == Current && Peek(2) == Current ? 3 : 1;

        private bool AtClosingQuote(char quote, int length)
            => Current == quote && (length == 1 || (Peek(1) == quote && Peek(2) == quote));

        private char ReadUnicodeEscape(int start, int line, int column)
        {
            if (_position + 4 > _source.Length)
            {
                ReportError(1007, CreateSpan(start, _position - start, line, column));
                return 'u';
            }

            string digits = _source.Substring(_position, 4);
            int value;
            if (!int.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
            {
                ReportError(1007, CreateSpan(start, _position + 4 - start, line, column));
                return 'u';
            }

            for (int index = 0; index < 4; index++)
            {
                Advance();
            }

            return (char)value;
        }

        private void ReadPunctuationOrOperator()
        {
            int start = _position;
            int line = _line;
            int column = _column;
            char current = Current;
            ArtiTokenKind kind;
            char second;

            switch (current)
            {
                case '{': kind = ArtiTokenKind.LeftBrace; break;
                case '}': kind = ArtiTokenKind.RightBrace; break;
                case '(': kind = ArtiTokenKind.LeftParen; break;
                case ')': kind = ArtiTokenKind.RightParen; break;
                case '[': kind = ArtiTokenKind.LeftBracket; break;
                case ']': kind = ArtiTokenKind.RightBracket; break;
                case ',': kind = ArtiTokenKind.Comma; break;
                case ':': kind = ArtiTokenKind.Colon; break;
                case '.': kind = ArtiTokenKind.Dot; break;
                case ';': kind = ArtiTokenKind.Semicolon; break;
                case '+': kind = ArtiTokenKind.Plus; break;
                case '-': kind = ArtiTokenKind.Minus; break;
                case '*': kind = ArtiTokenKind.Star; break;
                case '/': kind = ArtiTokenKind.Slash; break;
                case '%': kind = ArtiTokenKind.Percent; break;
                case '!': kind = ArtiTokenKind.Bang; break;
                case '=': kind = ArtiTokenKind.Equal; break;
                case '<': kind = ArtiTokenKind.Less; break;
                case '>': kind = ArtiTokenKind.Greater; break;
                case '&': kind = ArtiTokenKind.Unknown; break;
                case '|': kind = ArtiTokenKind.Pipe; break;
                case '?': kind = ArtiTokenKind.Unknown; break;
                default:
                    ReportError(1000, CurrentSpan(1), current);
                    Advance();
                    return;
            }

            second = Peek(1);
            if ((current == '+' || current == '-' || current == '*' || current == '/' || current == '%') && second == '=')
            {
                kind = current == '+' ? ArtiTokenKind.PlusEqual : current == '-' ? ArtiTokenKind.MinusEqual : current == '*' ? ArtiTokenKind.StarEqual : current == '/' ? ArtiTokenKind.SlashEqual : ArtiTokenKind.PercentEqual;
                Advance();
            }
            else if (current == '=' && second == '=')
            {
                kind = ArtiTokenKind.EqualEqual;
                Advance();
            }
            else if (current == '!' && second == '=')
            {
                kind = ArtiTokenKind.BangEqual;
                Advance();
            }
            else if (current == '<' && second == '=')
            {
                kind = ArtiTokenKind.LessEqual;
                Advance();
            }
            else if (current == '>' && second == '=')
            {
                kind = ArtiTokenKind.GreaterEqual;
                Advance();
            }
            else if (current == '&' && second == '&')
            {
                kind = ArtiTokenKind.AndAnd;
                Advance();
            }
            else if (current == '|' && second == '|')
            {
                kind = ArtiTokenKind.OrOr;
                Advance();
            }
            else if (current == '?' && second == '?')
            {
                kind = ArtiTokenKind.QuestionQuestion;
                Advance();
            }
            else if (current == '&' || current == '?')
            {
                ReportError(1008, CurrentSpan(1));
            }

            Advance();
            _result.Tokens.Add(new ArtiToken(
                kind,
                _source.Substring(start, _position - start),
                null,
                CreateSpan(start, _position - start, line, column)));
        }

        private void SkipLineComment()
        {
            Advance();
            Advance();
            while (!End && Current != '\r' && Current != '\n')
            {
                Advance();
            }
        }

        private ArtiSourceSpan CurrentSpan(int length)
        {
            return CreateSpan(_position, length, _line, _column);
        }

        private ArtiSourceSpan CreateSpan(int localOffset, int length, int line, int column)
        {
            return new ArtiSourceSpan(_sourceOffset + localOffset, length, line, column);
        }

        private void Advance()
        {
            if (End)
            {
                return;
            }

            if (Current == '\r')
            {
                _position++;
                if (!End && Current == '\n')
                {
                    _position++;
                }

                _line++;
                _column = 1;
                return;
            }

            if (Current == '\n')
            {
                _position++;
                _line++;
                _column = 1;
                return;
            }

            _position++;
            _column++;
        }

        private char Peek(int offset)
        {
            int index = _position + offset;
            return index >= 0 && index < _source.Length ? _source[index] : '\0';
        }

        private bool End
        {
            get { return _position >= _source.Length; }
        }

        private char Current
        {
            get { return End ? '\0' : _source[_position]; }
        }

        private static bool IsIdentifierStart(char value)
        {
            return char.IsLetter(value) || value == '_';
        }

        private static bool IsIdentifierPart(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }
    }
}
