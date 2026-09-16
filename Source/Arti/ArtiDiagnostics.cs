using System;
using System.Collections.Generic;
using System.Globalization;

namespace AdvancedRimTalk.Arti
{
    public enum ArtiDiagnosticLanguage
    {
        ChineseSimplified,
        English
    }

    public sealed class ArtiDiagnosticDefinition
    {
        public ArtiDiagnosticDefinition(int number, string messageZhCn, string messageEn)
        {
            Number = number;
            Code = "ART" + number.ToString("D4", CultureInfo.InvariantCulture);
            MessageZhCn = messageZhCn ?? string.Empty;
            MessageEn = messageEn ?? string.Empty;
        }

        public int Number { get; }
        public string Code { get; }
        public string MessageZhCn { get; }
        public string MessageEn { get; }
    }

    public static class ArtiDiagnosticCatalog
    {
        private static readonly IDictionary<int, ArtiDiagnosticDefinition> Definitions =
            new Dictionary<int, ArtiDiagnosticDefinition>
            {
                { 1000, Define(1000, "无法识别的字符 '{0}'。", "Unrecognized character '{0}'.") },
                { 1001, Define(1001, "Arti 目前只支持 // 行注释。", "Arti currently supports only // line comments.") },
                { 1002, Define(1002, "数字的指数部分缺少数字。", "The numeric exponent is missing digits.") },
                { 1003, Define(1003, "无法解析浮点数字。", "The floating-point number could not be parsed.") },
                { 1004, Define(1004, "整数超出支持范围。", "The integer is outside the supported range.") },
                { 1005, Define(1005, "不支持的字符串转义 \\{0}。", "Unsupported string escape \\{0}.") },
                { 1006, Define(1006, "字符串缺少结束引号。", "The string is missing its closing quote.") },
                { 1007, Define(1007, "Unicode 转义需要四位十六进制数字。", "A Unicode escape requires four hexadecimal digits.") },
                { 1008, Define(1008, "运算符必须写成完整形式。", "The operator must use its complete form.") },
                { 1009, Define(1009, "字符串插值缺少匹配的大括号；字面大括号请写成 {{ 或 }}。", "Interpolation braces must match; use {{ or }} for literal braces.") },

                { 2001, Define(2001, "语句之间需要换行或分号。", "Statements must be separated by a newline or semicolon.") },
                { 2002, Define(2002, "group 后只能跟 use。", "group must be followed by use.") },
                { 2003, Define(2003, "const use 是草案兼容写法，正式语法建议使用 group use。", "const use is a draft compatibility form; use group use in the formal syntax.") },
                { 2004, Define(2004, "代码块需要使用大括号包围。", "A code block must be enclosed in braces.") },
                { 2005, Define(2005, "需要一个表达式。", "An expression is required.") },
                { 2006, Define(2006, "对象成员需要名称。", "An object member requires a name.") },
                { 2007, Define(2007, "需要一个有效的标识符。", "A valid identifier is required.") },
                { 2008, Define(2008, "代码块缺少结束大括号。", "The code block is missing its closing brace.") },
                { 2009, Define(2009, "变量声明需要 = 和初始值。", "A variable declaration requires = and an initializer.") },
                { 2010, Define(2010, "函数声明需要 fn。", "A function declaration requires fn.") },
                { 2011, Define(2011, "函数名称后需要参数括号。", "A function declaration needs a parameter list after its name.") },
                { 2012, Define(2012, "函数参数列表缺少结束括号。", "The function parameter list is missing its closing parenthesis.") },
                { 2013, Define(2013, "for 循环需要 in。", "A for loop requires in.") },
                { 2014, Define(2014, "索引表达式缺少结束方括号。", "The index expression is missing its closing bracket.") },
                { 2015, Define(2015, "函数调用缺少结束括号。", "The function call is missing its closing parenthesis.") },
                { 2016, Define(2016, "表达式缺少结束括号。", "The expression is missing its closing parenthesis.") },
                { 2017, Define(2017, "数组缺少结束方括号。", "The array is missing its closing bracket.") },
                { 2018, Define(2018, "对象成员名称后需要冒号。", "An object member name must be followed by a colon.") },
                { 2019, Define(2019, "对象缺少结束大括号。", "The object is missing its closing brace.") },
                { 2020, Define(2020, "Arti 代码块缺少结束标记 %}}。", "The Arti code block is missing its closing %}} marker.") },

                { 3000, Define(3000, "不能分析空的 Arti 程序。", "A null Arti program cannot be analyzed.") },
                { 3001, Define(3001, "名称 '{0}' 在当前作用域中重复声明。", "The name '{0}' is declared more than once in the current scope.") },
                { 3002, Define(3002, "group use 只能出现在 Prompt 组的顶层。", "group use is only allowed at the top level of a Prompt group.") },
                { 3003, Define(3003, "函数参数 '{0}' 重复声明。", "The function parameter '{0}' is declared more than once.") },
                { 3004, Define(3004, "return 只能出现在函数内部。", "return is only allowed inside a function.") },
                { 3005, Define(3005, "break 只能出现在循环内部。", "break is only allowed inside a loop.") },
                { 3006, Define(3006, "continue 只能出现在循环内部。", "continue is only allowed inside a loop.") },
                { 3007, Define(3007, "赋值目标 '{0}' 尚未声明。", "The assignment target '{0}' has not been declared.") },
                { 3008, Define(3008, "名称 '{0}' 不能被赋值。", "The name '{0}' cannot be assigned to.") },
                { 3009, Define(3009, "名称 '{0}' 未声明。", "The name '{0}' has not been declared.") },
                { 3010, Define(3010, "模块名称不能为空。", "A module name cannot be empty.") },
                { 3011, Define(3011, "找不到必需模块 '{0}'。", "The required module '{0}' could not be found.") },
                { 3012, Define(3012, "必需模块 '{0}' 未 active 或没有可用 Prompt API。", "The required module '{0}' is not active or has no available Prompt API.") },
                { 3013, Define(3013, "模块别名 '{0}' 会覆盖内置名称。", "The module alias '{0}' would shadow a builtin name.") },
                { 3014, Define(3014, "赋值目标必须是已声明的变量名。", "An assignment target must be a declared variable name.") },
                { 3015, Define(3015, "模块别名 '{0}' 不是有效的标识符，请使用 as 指定别名。", "The module alias '{0}' is not a valid identifier; specify an alias with as.") },
                { 3016, Define(3016, "const 必须使用静态表达式；动态值请通过函数获取。", "const requires a static expression; use a function for dynamic values.") },

                { 4000, Define(4000, "Arti 运行时错误：{0}", "Arti runtime error: {0}") }
            };

        public static IEnumerable<ArtiDiagnosticDefinition> All
        {
            get { return Definitions.Values; }
        }

        public static ArtiDiagnosticDefinition GetDefinition(int number)
        {
            ArtiDiagnosticDefinition definition;
            return Definitions.TryGetValue(number, out definition) ? definition : null;
        }

        public static ArtiDiagnostic Create(ArtiDiagnosticSeverity severity, int number, ArtiSourceSpan span, params object[] arguments)
        {
            ArtiDiagnosticDefinition definition = GetDefinition(number);
            string code = definition == null ? FormatCode(number) : definition.Code;
            string messageZhCn;
            string messageEn;
            if (definition == null)
            {
                messageZhCn = "未定义的诊断 " + code + "。";
                messageEn = "Undefined diagnostic " + code + ".";
            }
            else
            {
                messageZhCn = Format(definition.MessageZhCn, arguments);
                messageEn = Format(definition.MessageEn, arguments);
            }

            return new ArtiDiagnostic(severity, code, messageZhCn, messageEn, span);
        }

        private static ArtiDiagnosticDefinition Define(int number, string messageZhCn, string messageEn)
        {
            return new ArtiDiagnosticDefinition(number, messageZhCn, messageEn);
        }

        private static string Format(string format, object[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return format;
            }

            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        private static string FormatCode(int number)
        {
            return "ART" + number.ToString("D4", CultureInfo.InvariantCulture);
        }
    }

    public abstract class ArtiDiagnosticReporter
    {
        protected readonly List<ArtiDiagnostic> ReportedDiagnostics = new List<ArtiDiagnostic>();

        protected void ClearDiagnostics()
        {
            ReportedDiagnostics.Clear();
        }

        protected List<ArtiDiagnostic> SnapshotDiagnostics()
        {
            return new List<ArtiDiagnostic>(ReportedDiagnostics);
        }

        protected void AddDiagnostic(ArtiDiagnostic diagnostic)
        {
            if (diagnostic != null)
            {
                ReportedDiagnostics.Add(diagnostic);
            }
        }

        protected void ReportError(int code, ArtiSourceSpan span, params object[] arguments)
        {
            AddDiagnostic(ArtiDiagnosticCatalog.Create(ArtiDiagnosticSeverity.Error, code, span, arguments));
        }

        protected void ReportError(int code, params object[] arguments)
        {
            ReportError(code, ArtiSourceSpan.Empty, arguments);
        }

        protected void ReportWarning(int code, ArtiSourceSpan span, params object[] arguments)
        {
            AddDiagnostic(ArtiDiagnosticCatalog.Create(ArtiDiagnosticSeverity.Warning, code, span, arguments));
        }

        protected void ReportWarning(int code, params object[] arguments)
        {
            ReportWarning(code, ArtiSourceSpan.Empty, arguments);
        }
    }
}
