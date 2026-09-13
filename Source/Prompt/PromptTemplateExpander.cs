using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AdvancedRimTalk.Prompt
{
    public interface IPromptRandom
    {
        int NextInt(int minInclusive, int maxInclusive);

        double NextDouble(double minInclusive, double maxInclusive);
    }

    public sealed class SystemPromptRandom : IPromptRandom
    {
        private static readonly Random Random = new Random();
        private static readonly object SyncRoot = new object();

        public int NextInt(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            long range = (long)maxInclusive - minInclusive + 1L;
            lock (SyncRoot)
            {
                long offset = (long)(Random.NextDouble() * range);
                if (offset >= range)
                {
                    offset = range - 1L;
                }

                return (int)((long)minInclusive + offset);
            }
        }

        public double NextDouble(double minInclusive, double maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            }

            if (maxInclusive == minInclusive)
            {
                return minInclusive;
            }

            lock (SyncRoot)
            {
                return minInclusive + ((maxInclusive - minInclusive) * Random.NextDouble());
            }
        }
    }

    public sealed class PromptExpansionResult
    {
        internal PromptExpansionResult(string template)
        {
            Template = template ?? string.Empty;
            Slots = new Dictionary<string, string>(StringComparer.Ordinal);
            Errors = new List<string>();
        }

        public string Template { get; internal set; }

        public int ExpandedCount { get; internal set; }

        public IDictionary<string, string> Slots { get; }

        public IList<string> Errors { get; }

        public string Restore(string renderedText)
        {
            if (renderedText == null || Slots.Count == 0)
            {
                return renderedText;
            }

            string restored = renderedText;
            foreach (KeyValuePair<string, string> slot in Slots)
            {
                restored = restored.Replace(slot.Key, slot.Value ?? string.Empty);
            }

            return restored;
        }
    }

    public static class PromptTemplateExpander
    {
        private const string Open = "{{";
        private const string Close = "}}";

        public static PromptExpansionResult Expand(string templateText, PromptSnapshot snapshot, IPromptRandom random = null)
        {
            PromptExpansionResult result = new PromptExpansionResult(templateText);
            if (string.IsNullOrEmpty(templateText))
            {
                return result;
            }

            random = random ?? new SystemPromptRandom();
            StringBuilder expanded = new StringBuilder(templateText.Length);
            int cursor = 0;
            int slotIndex = 0;

            while (cursor < templateText.Length)
            {
                int openIndex = templateText.IndexOf(Open, cursor, StringComparison.Ordinal);
                if (openIndex < 0)
                {
                    expanded.Append(templateText, cursor, templateText.Length - cursor);
                    break;
                }

                expanded.Append(templateText, cursor, openIndex - cursor);
                int closeIndex = templateText.IndexOf(Close, openIndex + Open.Length, StringComparison.Ordinal);
                if (closeIndex < 0)
                {
                    expanded.Append(templateText, openIndex, templateText.Length - openIndex);
                    break;
                }

                string expression = templateText.Substring(
                    openIndex + Open.Length,
                    closeIndex - openIndex - Open.Length);

                if (!IsAdvancedExpression(expression))
                {
                    expanded.Append(templateText, openIndex, closeIndex + Close.Length - openIndex);
                }
                else
                {
                    result.ExpandedCount++;
                    string value;
                    try
                    {
                        object evaluated = new ExpressionParser(expression, snapshot, random).Parse();
                        value = PromptValue.ToText(evaluated);
                    }
                    catch (Exception exception)
                    {
                        string error = FormatError(expression, exception);
                        result.Errors.Add(error);
                        value = "[AdvancedRimTalk placeholder error: " + exception.Message + "]";
                    }

                    string slot = CreateSlot(slotIndex++);
                    result.Slots.Add(slot, value);
                    expanded.Append(slot);
                }

                cursor = closeIndex + Close.Length;
            }

            result.Template = expanded.ToString();
            return result;
        }

        public static string ExpandToText(string templateText, PromptSnapshot snapshot, IPromptRandom random = null)
        {
            PromptExpansionResult result = Expand(templateText, snapshot, random);
            return result.Restore(result.Template);
        }

        public static bool HasAdvancedExpressions(string templateText)
        {
            if (string.IsNullOrEmpty(templateText))
            {
                return false;
            }

            int cursor = 0;
            while (cursor < templateText.Length)
            {
                int openIndex = templateText.IndexOf(Open, cursor, StringComparison.Ordinal);
                if (openIndex < 0)
                {
                    return false;
                }

                int closeIndex = templateText.IndexOf(Close, openIndex + Open.Length, StringComparison.Ordinal);
                if (closeIndex < 0)
                {
                    return false;
                }

                string expression = templateText.Substring(
                    openIndex + Open.Length,
                    closeIndex - openIndex - Open.Length);
                if (IsAdvancedExpression(expression))
                {
                    return true;
                }

                cursor = closeIndex + Close.Length;
            }

            return false;
        }

        private static bool IsAdvancedExpression(string expression)
        {
            return (expression ?? string.Empty).TrimStart().StartsWith("art.", StringComparison.OrdinalIgnoreCase);
        }

        private static string CreateSlot(int index)
        {
            return "\uE000AdvancedRimTalkSlot" + index.ToString(CultureInfo.InvariantCulture) + "\uE001";
        }

        private static string FormatError(string expression, Exception exception)
        {
            string compactExpression = (expression ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (compactExpression.Length > 180)
            {
                compactExpression = compactExpression.Substring(0, 180) + "...";
            }

            return compactExpression + ": " + exception.Message;
        }

        private sealed class ExpressionParser
        {
            private readonly string _text;
            private readonly PromptSnapshot _snapshot;
            private readonly IPromptRandom _random;
            private int _position;

            public ExpressionParser(string text, PromptSnapshot snapshot, IPromptRandom random)
            {
                _text = text ?? string.Empty;
                _snapshot = snapshot ?? new PromptSnapshot();
                _random = random;
            }

            public object Parse()
            {
                SkipWhitespace();
                if (End)
                {
                    throw new FormatException("empty expression");
                }

                object value;
                if (IsIdentifierStart(Current))
                {
                    string name = ParseIdentifier();
                    SkipWhitespace();
                    if (Consume('('))
                    {
                        value = Invoke(name, ParseCommaArguments());
                    }
                    else if (IsFunction(name))
                    {
                        value = Invoke(name, ParseWhitespaceArguments());
                    }
                    else
                    {
                        value = Resolve(name);
                    }
                }
                else
                {
                    value = ParseValue();
                }

                SkipWhitespace();
                if (!End)
                {
                    throw new FormatException("unexpected input near '" + _text.Substring(_position) + "'");
                }

                return value;
            }

            private object ParseValue()
            {
                SkipWhitespace();
                if (End)
                {
                    throw new FormatException("expected a value");
                }

                char current = Current;
                if (current == '"' || current == '\'')
                {
                    return ParseString(current);
                }

                if (current == '(')
                {
                    _position++;
                    object grouped = ParseValue();
                    SkipWhitespace();
                    Expect(')');
                    return grouped;
                }

                if (IsNumberStart())
                {
                    return ParseNumber();
                }

                if (IsIdentifierStart(current))
                {
                    string name = ParseIdentifier();
                    SkipWhitespace();
                    if (Consume('('))
                    {
                        return Invoke(name, ParseCommaArguments());
                    }

                    if (name.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (name.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (name.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    return Resolve(name);
                }

                throw new FormatException("expected a value near '" + _text.Substring(_position) + "'");
            }

            private List<object> ParseCommaArguments()
            {
                List<object> arguments = new List<object>();
                SkipWhitespace();
                if (Consume(')'))
                {
                    return arguments;
                }

                while (true)
                {
                    arguments.Add(ParseValue());
                    SkipWhitespace();
                    if (Consume(','))
                    {
                        continue;
                    }

                    Expect(')');
                    return arguments;
                }
            }

            private List<object> ParseWhitespaceArguments()
            {
                List<object> arguments = new List<object>();
                while (true)
                {
                    SkipWhitespace();
                    if (End)
                    {
                        return arguments;
                    }

                    arguments.Add(ParseValue());
                    SkipWhitespace();
                    Consume(',');
                }
            }

            private object ParseString(char quote)
            {
                _position++;
                StringBuilder value = new StringBuilder();
                while (!End)
                {
                    char current = _text[_position++];
                    if (current == quote)
                    {
                        return value.ToString();
                    }

                    if (current != '\\')
                    {
                        value.Append(current);
                        continue;
                    }

                    if (End)
                    {
                        throw new FormatException("unterminated string literal");
                    }

                    char escaped = _text[_position++];
                    switch (escaped)
                    {
                        case 'n':
                            value.Append('\n');
                            break;
                        case 'r':
                            value.Append('\r');
                            break;
                        case 't':
                            value.Append('\t');
                            break;
                        case '\\':
                            value.Append('\\');
                            break;
                        case '"':
                            value.Append('"');
                            break;
                        case '\'':
                            value.Append('\'');
                            break;
                        default:
                            value.Append(escaped);
                            break;
                    }
                }

                throw new FormatException("unterminated string literal");
            }

            private object ParseNumber()
            {
                int start = _position;
                if (Current == '+' || Current == '-')
                {
                    _position++;
                }

                while (!End && char.IsDigit(Current))
                {
                    _position++;
                }

                bool floatingPoint = false;
                if (!End && Current == '.')
                {
                    floatingPoint = true;
                    _position++;
                    while (!End && char.IsDigit(Current))
                    {
                        _position++;
                    }
                }

                if (!End && (Current == 'e' || Current == 'E'))
                {
                    floatingPoint = true;
                    _position++;
                    if (!End && (Current == '+' || Current == '-'))
                    {
                        _position++;
                    }

                    while (!End && char.IsDigit(Current))
                    {
                        _position++;
                    }
                }

                string number = _text.Substring(start, _position - start);
                if (!floatingPoint)
                {
                    int integer;
                    if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
                    {
                        return integer;
                    }
                }

                double decimalNumber;
                if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out decimalNumber))
                {
                    return decimalNumber;
                }

                throw new FormatException("invalid number '" + number + "'");
            }

            private string ParseIdentifier()
            {
                int start = _position;
                while (!End && IsIdentifierPart(Current))
                {
                    _position++;
                }

                return _text.Substring(start, _position - start);
            }

            private object Resolve(string name)
            {
                if (!name.StartsWith("art.", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException("unknown value '" + name + "'");
                }

                object value;
                if (_snapshot.TryGetValue(name.Substring(4), out value))
                {
                    return value;
                }

                throw new FormatException("unknown placeholder '" + name + "'");
            }

            private object Invoke(string name, List<object> arguments)
            {
                string normalized = name.ToLowerInvariant();
                if (normalized == "art.random_int" || normalized == "art.randint" || normalized == "art.random")
                {
                    RequireArgumentCount(name, arguments, 2);
                    return _random.NextInt(ToInt(arguments[0]), ToInt(arguments[1]));
                }

                if (normalized == "art.random_float")
                {
                    RequireArgumentCount(name, arguments, 2);
                    return _random.NextDouble(ToDouble(arguments[0]), ToDouble(arguments[1]));
                }

                if (normalized == "art.choose")
                {
                    if (arguments.Count == 0)
                    {
                        throw new FormatException("art.choose needs at least one option");
                    }

                    return arguments[_random.NextInt(0, arguments.Count - 1)];
                }

                if (normalized == "art.default")
                {
                    RequireArgumentCount(name, arguments, 2);
                    return PromptValue.IsBlank(arguments[0]) ? arguments[1] : arguments[0];
                }

                if (normalized == "art.coalesce")
                {
                    foreach (object argument in arguments)
                    {
                        if (!PromptValue.IsBlank(argument))
                        {
                            return argument;
                        }
                    }

                    return null;
                }

                if (normalized == "art.join")
                {
                    if (arguments.Count < 1)
                    {
                        throw new FormatException("art.join needs a separator");
                    }

                    string separator = PromptValue.ToText(arguments[0]);
                    StringBuilder joined = new StringBuilder();
                    for (int index = 1; index < arguments.Count; index++)
                    {
                        if (index > 1)
                        {
                            joined.Append(separator);
                        }

                        joined.Append(PromptValue.ToText(arguments[index]));
                    }

                    return joined.ToString();
                }

                throw new FormatException("unknown function '" + name + "'");
            }

            private static bool IsFunction(string name)
            {
                string normalized = name.ToLowerInvariant();
                return normalized == "art.random_int" || normalized == "art.randint" || normalized == "art.random" || normalized == "art.random_float" || normalized == "art.choose" || normalized == "art.default" || normalized == "art.coalesce" || normalized == "art.join";
            }

            private static void RequireArgumentCount(string name, List<object> arguments, int expected)
            {
                if (arguments.Count != expected)
                {
                    throw new FormatException(name + " expects " + expected.ToString(CultureInfo.InvariantCulture) + " arguments");
                }
            }

            private static int ToInt(object value)
            {
                if (value is int integer)
                {
                    return integer;
                }

                if (value is double decimalNumber && decimalNumber >= int.MinValue && decimalNumber <= int.MaxValue)
                {
                    return Convert.ToInt32(decimalNumber, CultureInfo.InvariantCulture);
                }

                int parsed;
                if (int.TryParse(PromptValue.ToText(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }

                throw new FormatException("expected an integer, got '" + PromptValue.ToText(value) + "'");
            }

            private static double ToDouble(object value)
            {
                if (value is double decimalNumber)
                {
                    return decimalNumber;
                }

                if (value is int integer)
                {
                    return integer;
                }

                double parsed;
                if (double.TryParse(PromptValue.ToText(value), NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    return parsed;
                }

                throw new FormatException("expected a number, got '" + PromptValue.ToText(value) + "'");
            }

            private bool IsNumberStart()
            {
                if (char.IsDigit(Current) || Current == '.')
                {
                    return true;
                }

                if ((Current == '+' || Current == '-') && _position + 1 < _text.Length)
                {
                    return char.IsDigit(_text[_position + 1]) || _text[_position + 1] == '.';
                }

                return false;
            }

            private void SkipWhitespace()
            {
                while (!End && char.IsWhiteSpace(Current))
                {
                    _position++;
                }
            }

            private bool Consume(char expected)
            {
                if (!End && Current == expected)
                {
                    _position++;
                    return true;
                }

                return false;
            }

            private void Expect(char expected)
            {
                if (!Consume(expected))
                {
                    throw new FormatException("expected '" + expected + "'");
                }
            }

            private bool End => _position >= _text.Length;

            private char Current => End ? '\0' : _text[_position];

            private static bool IsIdentifierStart(char value)
            {
                return char.IsLetter(value) || value == '_';
            }

            private static bool IsIdentifierPart(char value)
            {
                return char.IsLetterOrDigit(value) || value == '_' || value == '.';
            }
        }
    }

    internal static class PromptValue
    {
        public static bool IsBlank(object value)
        {
            string text = value as string;
            return value == null || (text != null && string.IsNullOrWhiteSpace(text));
        }

        public static string ToText(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is string text)
            {
                return text;
            }

            if (value is bool boolean)
            {
                return boolean ? "true" : "false";
            }

            IFormattable formattable = value as IFormattable;
            return formattable == null ? value.ToString() : formattable.ToString(null, CultureInfo.InvariantCulture);
        }
    }
}
