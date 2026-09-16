using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AdvancedRimTalk.Arti
{
    public sealed class ArtiParseResult
    {
        public ArtiParseResult(string source, ArtiProgram program, IList<ArtiToken> tokens, IList<ArtiDiagnostic> diagnostics)
        {
            Source = source ?? string.Empty;
            Program = program;
            Tokens = tokens;
            Diagnostics = diagnostics;
        }

        public string Source { get; }
        public ArtiProgram Program { get; }
        public IList<ArtiToken> Tokens { get; }
        public IList<ArtiDiagnostic> Diagnostics { get; }
        public bool HasErrors { get { return Diagnostics.Any(diagnostic => diagnostic.Severity == ArtiDiagnosticSeverity.Error); } }
    }

    public sealed class ArtiParser : ArtiDiagnosticReporter
    {
        private IList<ArtiToken> _tokens;
        private int _index;

        public ArtiParseResult Parse(string source)
        {
            return Parse(source, 0, 1, 1);
        }

        public ArtiParseResult Parse(string source, int sourceOffset, int startLine, int startColumn)
        {
            source = source ?? string.Empty;
            ArtiLexResult lexResult = new ArtiLexer().Lex(source, sourceOffset, startLine, startColumn);
            _tokens = lexResult.Tokens;
            _index = 0;
            ClearDiagnostics();
            foreach (ArtiDiagnostic diagnostic in lexResult.Diagnostics)
            {
                AddDiagnostic(diagnostic);
            }

            ArtiProgram program = new ArtiProgram(new ArtiSourceSpan(sourceOffset, source.Length, startLine, startColumn));
            ConsumeSeparators();
            while (!Check(ArtiTokenKind.EndOfFile))
            {
                ArtiStatement statement = ParseStatement();
                if (statement != null)
                {
                    program.Statements.Add(statement);
                }

                if (!ConsumeSeparators() && !Check(ArtiTokenKind.EndOfFile))
                {
                    ReportError(2001, Current.Span);
                    SynchronizeStatement();
                }
            }

            return new ArtiParseResult(source, program, _tokens, SnapshotDiagnostics());
        }

        private ArtiStatement ParseStatement()
        {
            switch (Current.Kind)
            {
                case ArtiTokenKind.Use:
                    return ParseUse(false);
                case ArtiTokenKind.Group:
                    Advance();
                    if (!Match(ArtiTokenKind.Use))
                    {
                        ReportError(2002, Previous.Span);
                        return null;
                    }

                    return ParseUse(true, Previous.Span);
                case ArtiTokenKind.Const:
                    ArtiToken constToken = Advance();
                    if (Match(ArtiTokenKind.Use))
                    {
                        ReportWarning(2003, constToken.Span);
                        return ParseUse(true, Previous.Span);
                    }

                    return ParseVariableDeclaration(true, constToken);
                case ArtiTokenKind.Let:
                    return ParseVariableDeclaration(false, Advance());
                case ArtiTokenKind.Fn:
                    return ParseFunction();
                case ArtiTokenKind.If:
                    return ParseIf();
                case ArtiTokenKind.For:
                    return ParseFor();
                case ArtiTokenKind.While:
                    return ParseWhile();
                case ArtiTokenKind.Return:
                    return ParseReturn();
                case ArtiTokenKind.Break:
                    return new ArtiBreakStatement(Advance().Span);
                case ArtiTokenKind.Continue:
                    return new ArtiContinueStatement(Advance().Span);
                case ArtiTokenKind.LeftBrace:
                    return ParseBlock();
                default:
                    return ParseExpressionOrAssignment();
            }
        }

        private ArtiUseStatement ParseUse(bool isGroup, ArtiSourceSpan? consumedUseSpan = null)
        {
            ArtiToken useToken = consumedUseSpan.HasValue ? new ArtiToken(ArtiTokenKind.Use, "use", null, consumedUseSpan.Value) : Advance();
            bool isOptional = Match(ArtiTokenKind.Optional);
            string packageId = ParseQualifiedName();
            string alias = string.Empty;
            if (Match(ArtiTokenKind.As))
            {
                alias = ExpectIdentifier().Text;
            }

            if (string.IsNullOrEmpty(alias))
            {
                int separator = packageId.LastIndexOf('.');
                alias = separator >= 0 ? packageId.Substring(separator + 1) : packageId;
            }

            ArtiSourceSpan span = Combine(useToken.Span, Previous.Span);
            return new ArtiUseStatement(span, packageId, alias, isOptional, isGroup);
        }

        private ArtiStatement ParseVariableDeclaration(bool isConst, ArtiToken keyword)
        {
            ArtiToken name = ExpectIdentifier();
            if (!isConst && Check(ArtiTokenKind.Comma))
            {
                var targets = new ArtiArrayExpression(name.Span);
                targets.Items.Add(new ArtiNameExpression(name.Span, name.Text));
                while (Match(ArtiTokenKind.Comma))
                {
                    ArtiToken next = ExpectIdentifier();
                    targets.Items.Add(new ArtiNameExpression(next.Span, next.Text));
                }
                Expect(ArtiTokenKind.Equal, 2009);
                ArtiExpression values = ParseExpressionList();
                return new ArtiUnpackDeclarationStatement(Combine(keyword.Span, values.Span), targets, values);
            }
            Expect(ArtiTokenKind.Equal, 2009);
            ArtiExpression value = ParseExpressionList();
            return new ArtiVariableDeclarationStatement(Combine(keyword.Span, value.Span), name.Text, isConst, value);
        }

        private ArtiFunctionDeclarationStatement ParseFunction()
        {
            ArtiToken fnToken = Expect(ArtiTokenKind.Fn, 2010);
            ArtiToken name = ExpectIdentifier();
            Expect(ArtiTokenKind.LeftParen, 2011);
            List<string> parameters = new List<string>();
            SkipNewLines();
            if (!Check(ArtiTokenKind.RightParen))
            {
                while (!Check(ArtiTokenKind.EndOfFile) && !Check(ArtiTokenKind.RightParen))
                {
                    parameters.Add(ExpectIdentifier().Text);
                    SkipNewLines();
                    if (!Match(ArtiTokenKind.Comma))
                    {
                        break;
                    }

                    SkipNewLines();
                }
            }

            Expect(ArtiTokenKind.RightParen, 2012);
            SkipNewLines();
            ArtiBlockStatement body = ParseBlock();
            ArtiFunctionDeclarationStatement function = new ArtiFunctionDeclarationStatement(Combine(fnToken.Span, body.Span), name.Text, body);
            foreach (string parameter in parameters)
            {
                function.Parameters.Add(parameter);
            }

            return function;
        }

        private ArtiIfStatement ParseIf()
        {
            ArtiToken ifToken = Advance();
            ArtiIfStatement statement = new ArtiIfStatement(ifToken.Span);
            ArtiExpression condition = ParseExpression();
            SkipNewLines();
            ArtiBlockStatement body = ParseBlock();
            statement.Branches.Add(new ArtiIfBranch(Combine(condition.Span, body.Span), condition, body));

            while (true)
            {
                int savedIndex = _index;
                SkipNewLines();
                if (!Match(ArtiTokenKind.Else))
                {
                    _index = savedIndex;
                    break;
                }

                if (Match(ArtiTokenKind.If))
                {
                    ArtiExpression elseIfCondition = ParseExpression();
                    SkipNewLines();
                    ArtiBlockStatement elseIfBody = ParseBlock();
                    statement.Branches.Add(new ArtiIfBranch(Combine(elseIfCondition.Span, elseIfBody.Span), elseIfCondition, elseIfBody));
                    continue;
                }

                SkipNewLines();
                statement.ElseBody = ParseBlock();
                break;
            }

            ArtiSourceSpan endSpan = statement.ElseBody == null ? statement.Branches[statement.Branches.Count - 1].Span : statement.ElseBody.Span;
            statement.Span = Combine(ifToken.Span, endSpan);
            return statement;
        }

        private ArtiForStatement ParseFor()
        {
            ArtiToken forToken = Advance();
            ArtiToken variable = ExpectIdentifier();
            Expect(ArtiTokenKind.In, 2013);
            ArtiExpression source = ParseExpression();
            SkipNewLines();
            ArtiBlockStatement body = ParseBlock();
            return new ArtiForStatement(Combine(forToken.Span, body.Span), variable.Text, source, body);
        }

        private ArtiWhileStatement ParseWhile()
        {
            ArtiToken whileToken = Advance();
            ArtiExpression condition = ParseExpression();
            SkipNewLines();
            ArtiBlockStatement body = ParseBlock();
            return new ArtiWhileStatement(Combine(whileToken.Span, body.Span), condition, body);
        }

        private ArtiReturnStatement ParseReturn()
        {
            ArtiToken returnToken = Advance();
            ArtiExpression value = IsStatementTerminator(Current.Kind) ? null : ParseExpressionList();
            return new ArtiReturnStatement(Combine(returnToken.Span, value == null ? returnToken.Span : value.Span), value);
        }

        private ArtiBlockStatement ParseBlock()
        {
            if (!Check(ArtiTokenKind.LeftBrace))
            {
                ReportError(2004, Current.Span);
                return new ArtiBlockStatement(Current.Span);
            }

            ArtiToken leftBrace = Advance();
            ArtiBlockStatement block = new ArtiBlockStatement(leftBrace.Span);
            ConsumeSeparators();
            while (!Check(ArtiTokenKind.RightBrace) && !Check(ArtiTokenKind.EndOfFile))
            {
                ArtiStatement statement = ParseStatement();
                if (statement != null)
                {
                    block.Statements.Add(statement);
                }

                if (!ConsumeSeparators() && !Check(ArtiTokenKind.RightBrace) && !Check(ArtiTokenKind.EndOfFile))
                {
                    ReportError(2001, Current.Span);
                    SynchronizeStatement();
                }
            }

            ArtiToken rightBrace = Expect(ArtiTokenKind.RightBrace, 2008);
            block.Span = Combine(leftBrace.Span, rightBrace.Span);
            return block;
        }

        private ArtiStatement ParseExpressionOrAssignment()
        {
            ArtiExpression target = ParseExpressionList();
            if (IsAssignmentOperator(Current.Kind))
            {
                ArtiTokenKind op = Advance().Kind;
                ArtiExpression value = ParseExpressionList();
                return new ArtiAssignmentStatement(Combine(target.Span, value.Span), target, op, value);
            }

            return new ArtiExpressionStatement(target.Span, target);
        }

        private ArtiExpression ParseExpressionList()
        {
            ArtiExpression first = ParseExpression();
            if (!Check(ArtiTokenKind.Comma)) return first;
            // Multiple values use the existing ordered array representation.
            var values = new ArtiArrayExpression(first.Span);
            values.Items.Add(first);
            while (Match(ArtiTokenKind.Comma)) values.Items.Add(ParseExpression());
            values.Span = Combine(first.Span, values.Items[values.Items.Count - 1].Span);
            return values;
        }

        private ArtiExpression ParseExpression(int minimumPrecedence = 0)
        {
            ArtiExpression left = ParseUnary();
            while (true)
            {
                int precedence = GetPrecedence(Current.Kind);
                if (precedence < minimumPrecedence)
                {
                    break;
                }

                ArtiToken op = Advance();
                ArtiExpression right = ParseExpression(precedence + 1);
                left = new ArtiBinaryExpression(Combine(left.Span, right.Span), left, op.Kind, right);
            }

            return left;
        }

        private ArtiExpression ParseUnary()
        {
            if (Current.Kind == ArtiTokenKind.Bang || Current.Kind == ArtiTokenKind.Plus || Current.Kind == ArtiTokenKind.Minus)
            {
                ArtiToken op = Advance();
                ArtiExpression operand = ParseUnary();
                return new ArtiUnaryExpression(Combine(op.Span, operand.Span), op.Kind, operand);
            }

            return ParsePostfix(ParsePrimary());
        }

        private ArtiExpression ParsePostfix(ArtiExpression expression)
        {
            while (true)
            {
                if (Match(ArtiTokenKind.Dot))
                {
                    ArtiToken member = ExpectIdentifier();
                    expression = new ArtiMemberExpression(Combine(expression.Span, member.Span), expression, member.Text);
                    continue;
                }

                if (Match(ArtiTokenKind.LeftBracket))
                {
                    ArtiExpression index = ParseExpression();
                    ArtiToken rightBracket = Expect(ArtiTokenKind.RightBracket, 2014);
                    expression = new ArtiIndexExpression(Combine(expression.Span, rightBracket.Span), expression, index);
                    continue;
                }

                if (Match(ArtiTokenKind.LeftParen))
                {
                    ArtiCallExpression call = new ArtiCallExpression(Combine(expression.Span, Previous.Span), expression);
                    ParseArguments(call);
                    expression = call;
                    continue;
                }

                break;
            }

            return expression;
        }

        private void ParseArguments(ArtiCallExpression call)
        {
            SkipNewLines();
            if (Match(ArtiTokenKind.RightParen))
            {
                call.Span = Combine(call.Span, Previous.Span);
                return;
            }

            while (!Check(ArtiTokenKind.EndOfFile) && !Check(ArtiTokenKind.RightParen))
            {
                string name = null;
                if (Check(ArtiTokenKind.Identifier) && Peek(1).Kind == ArtiTokenKind.Colon)
                {
                    name = Advance().Text;
                    Advance();
                }

                ArtiExpression value = ParseExpression();
                call.Arguments.Add(new ArtiArgument(name, value));
                SkipNewLines();
                if (!Match(ArtiTokenKind.Comma))
                {
                    break;
                }

                SkipNewLines();
            }

            ArtiToken rightParen = Expect(ArtiTokenKind.RightParen, 2015);
            call.Span = Combine(call.Span, rightParen.Span);
        }

        private ArtiExpression ParsePrimary()
        {
            ArtiToken token = Current;
            switch (token.Kind)
            {
                case ArtiTokenKind.String:
                case ArtiTokenKind.Integer:
                case ArtiTokenKind.Float:
                    Advance();
                    return new ArtiLiteralExpression(token.Span, token.Value);
                case ArtiTokenKind.InterpolatedStringStart:
                    return ParseInterpolatedString();
                case ArtiTokenKind.True:
                    Advance();
                    return new ArtiLiteralExpression(token.Span, true);
                case ArtiTokenKind.False:
                    Advance();
                    return new ArtiLiteralExpression(token.Span, false);
                case ArtiTokenKind.Null:
                    Advance();
                    return new ArtiLiteralExpression(token.Span, null);
                case ArtiTokenKind.Identifier:
                    Advance();
                    return new ArtiNameExpression(token.Span, token.Text);
                case ArtiTokenKind.LeftParen:
                    Advance();
                    ArtiExpression grouped = ParseExpressionList();
                    Expect(ArtiTokenKind.RightParen, 2016);
                    return grouped;
                case ArtiTokenKind.LeftBracket:
                    return ParseArray();
                case ArtiTokenKind.LeftBrace:
                    return ParseObject();
                default:
                    ReportError(2005, token.Span);
                    if (!Check(ArtiTokenKind.EndOfFile))
                    {
                        Advance();
                    }

                    return new ArtiErrorExpression(token.Span);
            }
        }

        private ArtiInterpolatedStringExpression ParseInterpolatedString()
        {
            ArtiToken start = Advance();
            var result = new ArtiInterpolatedStringExpression(start.Span);
            while (!Check(ArtiTokenKind.InterpolatedStringEnd) && !Check(ArtiTokenKind.EndOfFile))
            {
                if (Check(ArtiTokenKind.String))
                {
                    ArtiToken text = Advance();
                    result.Parts.Add(new ArtiLiteralExpression(text.Span, text.Value));
                }
                else if (Match(ArtiTokenKind.InterpolationStart))
                {
                    result.Parts.Add(ParseExpression());
                    Expect(ArtiTokenKind.InterpolationEnd, 1009);
                }
                else
                {
                    ReportError(2005, Current.Span);
                    Advance();
                }
            }
            ArtiToken end = Expect(ArtiTokenKind.InterpolatedStringEnd, 1006);
            result.Span = Combine(start.Span, end.Span);
            return result;
        }

        private ArtiArrayExpression ParseArray()
        {
            ArtiToken leftBracket = Advance();
            ArtiArrayExpression array = new ArtiArrayExpression(leftBracket.Span);
            SkipNewLines();
            while (!Check(ArtiTokenKind.RightBracket) && !Check(ArtiTokenKind.EndOfFile))
            {
                array.Items.Add(ParseExpression());
                SkipNewLines();
                if (!Match(ArtiTokenKind.Comma))
                {
                    break;
                }

                SkipNewLines();
            }

            ArtiToken rightBracket = Expect(ArtiTokenKind.RightBracket, 2017);
            array.Span = Combine(leftBracket.Span, rightBracket.Span);
            return array;
        }

        private ArtiObjectExpression ParseObject()
        {
            ArtiToken leftBrace = Advance();
            ArtiObjectExpression obj = new ArtiObjectExpression(leftBrace.Span);
            SkipNewLines();
            while (!Check(ArtiTokenKind.RightBrace) && !Check(ArtiTokenKind.EndOfFile))
            {
                ArtiToken key = Current;
                if (!Match(ArtiTokenKind.Identifier) && !Match(ArtiTokenKind.String))
                {
                    ReportError(2006, Current.Span);
                    SynchronizeObjectMember();
                    continue;
                }

                Expect(ArtiTokenKind.Colon, 2018);
                ArtiExpression value = ParseExpression();
                obj.Members.Add(new ArtiObjectMember(key.Kind == ArtiTokenKind.String ? Convert.ToString(key.Value, CultureInfo.InvariantCulture) : key.Text, value));
                SkipNewLines();
                if (!Match(ArtiTokenKind.Comma))
                {
                    break;
                }

                SkipNewLines();
            }

            ArtiToken rightBrace = Expect(ArtiTokenKind.RightBrace, 2019);
            obj.Span = Combine(leftBrace.Span, rightBrace.Span);
            return obj;
        }

        private string ParseQualifiedName()
        {
            if (Check(ArtiTokenKind.String))
            {
                return Convert.ToString(Advance().Value, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            ArtiToken first = ExpectIdentifier();
            StringBuilder package = new StringBuilder(first.Text);
            while (Match(ArtiTokenKind.Dot))
            {
                package.Append('.');
                package.Append(ExpectIdentifier().Text);
            }

            return package.ToString();
        }

        private void SynchronizeStatement()
        {
            while (!Check(ArtiTokenKind.EndOfFile) && !Check(ArtiTokenKind.NewLine) && !Check(ArtiTokenKind.Semicolon) && !Check(ArtiTokenKind.RightBrace))
            {
                Advance();
            }
        }

        private void SynchronizeObjectMember()
        {
            while (!Check(ArtiTokenKind.EndOfFile) && !Check(ArtiTokenKind.RightBrace) && !Check(ArtiTokenKind.Comma) && !Check(ArtiTokenKind.NewLine))
            {
                Advance();
            }

            Match(ArtiTokenKind.Comma);
            SkipNewLines();
        }

        private ArtiToken ExpectIdentifier()
        {
            if (Check(ArtiTokenKind.Identifier))
            {
                return Advance();
            }

            ReportError(2007, Current.Span);
            return new ArtiToken(ArtiTokenKind.Identifier, string.Empty, string.Empty, Current.Span);
        }

        private ArtiToken Expect(ArtiTokenKind kind, int diagnosticCode)
        {
            if (Check(kind))
            {
                return Advance();
            }

            ReportError(diagnosticCode, Current.Span);
            return new ArtiToken(kind, string.Empty, null, Current.Span);
        }

        private bool Match(ArtiTokenKind kind)
        {
            if (!Check(kind))
            {
                return false;
            }

            Advance();
            return true;
        }

        private ArtiToken Advance()
        {
            ArtiToken token = Current;
            if (_index < _tokens.Count - 1)
            {
                _index++;
            }

            return token;
        }

        private ArtiToken Peek(int distance)
        {
            int index = _index + distance;
            return index >= 0 && index < _tokens.Count ? _tokens[index] : _tokens[_tokens.Count - 1];
        }

        private bool Check(ArtiTokenKind kind)
        {
            return Current.Kind == kind;
        }

        private ArtiToken Current
        {
            get { return _tokens[_index]; }
        }

        private ArtiToken Previous
        {
            get { return _tokens[Math.Max(0, _index - 1)]; }
        }

        private bool ConsumeSeparators()
        {
            bool consumed = false;
            while (Match(ArtiTokenKind.NewLine) || Match(ArtiTokenKind.Semicolon))
            {
                consumed = true;
            }

            return consumed;
        }

        private void SkipNewLines()
        {
            while (Match(ArtiTokenKind.NewLine))
            {
            }
        }

        private static bool IsStatementTerminator(ArtiTokenKind kind)
        {
            return kind == ArtiTokenKind.NewLine || kind == ArtiTokenKind.Semicolon || kind == ArtiTokenKind.RightBrace || kind == ArtiTokenKind.EndOfFile;
        }

        private static bool IsAssignmentOperator(ArtiTokenKind kind)
        {
            return kind == ArtiTokenKind.Equal || kind == ArtiTokenKind.PlusEqual || kind == ArtiTokenKind.MinusEqual || kind == ArtiTokenKind.StarEqual || kind == ArtiTokenKind.SlashEqual || kind == ArtiTokenKind.PercentEqual;
        }

        private static int GetPrecedence(ArtiTokenKind kind)
        {
            switch (kind)
            {
                case ArtiTokenKind.Pipe: return 1;
                case ArtiTokenKind.OrOr: return 2;
                case ArtiTokenKind.AndAnd: return 3;
                case ArtiTokenKind.QuestionQuestion: return 4;
                case ArtiTokenKind.EqualEqual:
                case ArtiTokenKind.BangEqual: return 5;
                case ArtiTokenKind.Less:
                case ArtiTokenKind.LessEqual:
                case ArtiTokenKind.Greater:
                case ArtiTokenKind.GreaterEqual: return 6;
                case ArtiTokenKind.Plus:
                case ArtiTokenKind.Minus: return 7;
                case ArtiTokenKind.Star:
                case ArtiTokenKind.Slash:
                case ArtiTokenKind.Percent: return 8;
                default: return -1;
            }
        }

        private static ArtiSourceSpan Combine(ArtiSourceSpan first, ArtiSourceSpan last)
        {
            int start = Math.Min(first.StartOffset, last.StartOffset);
            int end = Math.Max(first.EndOffset, last.EndOffset);
            return new ArtiSourceSpan(start, end - start, first.Line, first.Column);
        }

    }
}
