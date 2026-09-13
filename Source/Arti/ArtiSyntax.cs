using System;
using System.Collections.Generic;

namespace AdvancedRimTalk.Arti
{
    public struct ArtiSourceSpan
    {
        public ArtiSourceSpan(int startOffset, int length, int line, int column)
        {
            StartOffset = startOffset;
            Length = length;
            Line = line;
            Column = column;
        }

        public int StartOffset { get; }
        public int Length { get; }
        public int Line { get; }
        public int Column { get; }
        public int EndOffset { get { return StartOffset + Length; } }

        public static ArtiSourceSpan Empty
        {
            get { return new ArtiSourceSpan(0, 0, 1, 1); }
        }
    }

    public enum ArtiDiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class ArtiDiagnostic
    {
        public ArtiDiagnostic(ArtiDiagnosticSeverity severity, string code, string message, ArtiSourceSpan span)
            : this(severity, code, message, message, span)
        {
        }

        public ArtiDiagnostic(ArtiDiagnosticSeverity severity, string code, string messageZhCn, string messageEn, ArtiSourceSpan span)
        {
            Severity = severity;
            Code = code ?? string.Empty;
            MessageZhCn = messageZhCn ?? string.Empty;
            MessageEn = messageEn ?? string.Empty;
            Message = MessageZhCn;
            Span = span;
        }

        public ArtiDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string MessageZhCn { get; }
        public string MessageEn { get; }
        public ArtiSourceSpan Span { get; }

        public string GetMessage(ArtiDiagnosticLanguage language)
        {
            return language == ArtiDiagnosticLanguage.English ? MessageEn : MessageZhCn;
        }

        public override string ToString()
        {
            return Code + " (" + Span.Line + ":" + Span.Column + "): " + Message;
        }
    }

    public enum ArtiTokenKind
    {
        EndOfFile,
        NewLine,
        Identifier,
        String,
        Integer,
        Float,
        True,
        False,
        Null,
        Use,
        Optional,
        As,
        Group,
        Const,
        Let,
        Fn,
        If,
        Else,
        For,
        In,
        While,
        Return,
        Break,
        Continue,
        LeftBrace,
        RightBrace,
        LeftParen,
        RightParen,
        LeftBracket,
        RightBracket,
        Comma,
        Colon,
        Dot,
        Semicolon,
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Bang,
        Equal,
        PlusEqual,
        MinusEqual,
        StarEqual,
        SlashEqual,
        PercentEqual,
        EqualEqual,
        BangEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        AndAnd,
        OrOr,
        Pipe,
        QuestionQuestion,
        Unknown
    }

    public sealed class ArtiToken
    {
        public ArtiToken(ArtiTokenKind kind, string text, object value, ArtiSourceSpan span)
        {
            Kind = kind;
            Text = text ?? string.Empty;
            Value = value;
            Span = span;
        }

        public ArtiTokenKind Kind { get; }
        public string Text { get; }
        public object Value { get; }
        public ArtiSourceSpan Span { get; }
    }

    public abstract class ArtiNode
    {
        protected ArtiNode(ArtiSourceSpan span)
        {
            Span = span;
        }

        public ArtiSourceSpan Span { get; internal set; }
    }

    public sealed class ArtiProgram : ArtiNode
    {
        public ArtiProgram(ArtiSourceSpan span)
            : base(span)
        {
            Statements = new List<ArtiStatement>();
        }

        public IList<ArtiStatement> Statements { get; }
    }

    public abstract class ArtiStatement : ArtiNode
    {
        protected ArtiStatement(ArtiSourceSpan span)
            : base(span)
        {
        }
    }

    public sealed class ArtiBlockStatement : ArtiStatement
    {
        public ArtiBlockStatement(ArtiSourceSpan span)
            : base(span)
        {
            Statements = new List<ArtiStatement>();
        }

        public IList<ArtiStatement> Statements { get; }
    }

    public sealed class ArtiUseStatement : ArtiStatement
    {
        public ArtiUseStatement(ArtiSourceSpan span, string packageId, string alias, bool isOptional, bool isGroup)
            : base(span)
        {
            PackageId = packageId ?? string.Empty;
            Alias = alias ?? string.Empty;
            IsOptional = isOptional;
            IsGroup = isGroup;
        }

        public string PackageId { get; }
        public string Alias { get; }
        public bool IsOptional { get; }
        public bool IsGroup { get; }
    }

    public sealed class ArtiVariableDeclarationStatement : ArtiStatement
    {
        public ArtiVariableDeclarationStatement(ArtiSourceSpan span, string name, bool isConst, ArtiExpression value)
            : base(span)
        {
            Name = name ?? string.Empty;
            IsConst = isConst;
            Value = value;
        }

        public string Name { get; }
        public bool IsConst { get; }
        public ArtiExpression Value { get; }
    }

    public sealed class ArtiAssignmentStatement : ArtiStatement
    {
        public ArtiAssignmentStatement(ArtiSourceSpan span, ArtiExpression target, ArtiTokenKind op, ArtiExpression value)
            : base(span)
        {
            Target = target;
            Operator = op;
            Value = value;
        }

        public ArtiExpression Target { get; }
        public ArtiTokenKind Operator { get; }
        public ArtiExpression Value { get; }
    }

    public sealed class ArtiFunctionDeclarationStatement : ArtiStatement
    {
        public ArtiFunctionDeclarationStatement(ArtiSourceSpan span, string name, ArtiBlockStatement body)
            : base(span)
        {
            Name = name ?? string.Empty;
            Parameters = new List<string>();
            Body = body;
        }

        public string Name { get; }
        public IList<string> Parameters { get; }
        public ArtiBlockStatement Body { get; }
    }

    public sealed class ArtiIfBranch : ArtiNode
    {
        public ArtiIfBranch(ArtiSourceSpan span, ArtiExpression condition, ArtiBlockStatement body)
            : base(span)
        {
            Condition = condition;
            Body = body;
        }

        public ArtiExpression Condition { get; }
        public ArtiBlockStatement Body { get; }
    }

    public sealed class ArtiIfStatement : ArtiStatement
    {
        public ArtiIfStatement(ArtiSourceSpan span)
            : base(span)
        {
            Branches = new List<ArtiIfBranch>();
        }

        public IList<ArtiIfBranch> Branches { get; }
        public ArtiBlockStatement ElseBody { get; set; }
    }

    public sealed class ArtiForStatement : ArtiStatement
    {
        public ArtiForStatement(ArtiSourceSpan span, string variableName, ArtiExpression source, ArtiBlockStatement body)
            : base(span)
        {
            VariableName = variableName ?? string.Empty;
            Source = source;
            Body = body;
        }

        public string VariableName { get; }
        public ArtiExpression Source { get; }
        public ArtiBlockStatement Body { get; }
    }

    public sealed class ArtiWhileStatement : ArtiStatement
    {
        public ArtiWhileStatement(ArtiSourceSpan span, ArtiExpression condition, ArtiBlockStatement body)
            : base(span)
        {
            Condition = condition;
            Body = body;
        }

        public ArtiExpression Condition { get; }
        public ArtiBlockStatement Body { get; }
    }

    public sealed class ArtiReturnStatement : ArtiStatement
    {
        public ArtiReturnStatement(ArtiSourceSpan span, ArtiExpression value)
            : base(span)
        {
            Value = value;
        }

        public ArtiExpression Value { get; }
    }

    public sealed class ArtiBreakStatement : ArtiStatement
    {
        public ArtiBreakStatement(ArtiSourceSpan span)
            : base(span)
        {
        }
    }

    public sealed class ArtiContinueStatement : ArtiStatement
    {
        public ArtiContinueStatement(ArtiSourceSpan span)
            : base(span)
        {
        }
    }

    public sealed class ArtiExpressionStatement : ArtiStatement
    {
        public ArtiExpressionStatement(ArtiSourceSpan span, ArtiExpression expression)
            : base(span)
        {
            Expression = expression;
        }

        public ArtiExpression Expression { get; }
    }

    public abstract class ArtiExpression : ArtiNode
    {
        protected ArtiExpression(ArtiSourceSpan span)
            : base(span)
        {
        }
    }

    public sealed class ArtiErrorExpression : ArtiExpression
    {
        public ArtiErrorExpression(ArtiSourceSpan span)
            : base(span)
        {
        }
    }

    public sealed class ArtiLiteralExpression : ArtiExpression
    {
        public ArtiLiteralExpression(ArtiSourceSpan span, object value)
            : base(span)
        {
            Value = value;
        }

        public object Value { get; }
    }

    public sealed class ArtiNameExpression : ArtiExpression
    {
        public ArtiNameExpression(ArtiSourceSpan span, string name)
            : base(span)
        {
            Name = name ?? string.Empty;
        }

        public string Name { get; }
    }

    public sealed class ArtiMemberExpression : ArtiExpression
    {
        public ArtiMemberExpression(ArtiSourceSpan span, ArtiExpression target, string member)
            : base(span)
        {
            Target = target;
            Member = member ?? string.Empty;
        }

        public ArtiExpression Target { get; }
        public string Member { get; }
    }

    public sealed class ArtiIndexExpression : ArtiExpression
    {
        public ArtiIndexExpression(ArtiSourceSpan span, ArtiExpression target, ArtiExpression index)
            : base(span)
        {
            Target = target;
            Index = index;
        }

        public ArtiExpression Target { get; }
        public ArtiExpression Index { get; }
    }

    public sealed class ArtiArgument
    {
        public ArtiArgument(string name, ArtiExpression value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }
        public ArtiExpression Value { get; }
    }

    public sealed class ArtiCallExpression : ArtiExpression
    {
        public ArtiCallExpression(ArtiSourceSpan span, ArtiExpression target)
            : base(span)
        {
            Target = target;
            Arguments = new List<ArtiArgument>();
        }

        public ArtiExpression Target { get; }
        public IList<ArtiArgument> Arguments { get; }
    }

    public sealed class ArtiArrayExpression : ArtiExpression
    {
        public ArtiArrayExpression(ArtiSourceSpan span)
            : base(span)
        {
            Items = new List<ArtiExpression>();
        }

        public IList<ArtiExpression> Items { get; }
    }

    public sealed class ArtiObjectMember
    {
        public ArtiObjectMember(string name, ArtiExpression value)
        {
            Name = name ?? string.Empty;
            Value = value;
        }

        public string Name { get; }
        public ArtiExpression Value { get; }
    }

    public sealed class ArtiObjectExpression : ArtiExpression
    {
        public ArtiObjectExpression(ArtiSourceSpan span)
            : base(span)
        {
            Members = new List<ArtiObjectMember>();
        }

        public IList<ArtiObjectMember> Members { get; }
    }

    public sealed class ArtiUnaryExpression : ArtiExpression
    {
        public ArtiUnaryExpression(ArtiSourceSpan span, ArtiTokenKind op, ArtiExpression operand)
            : base(span)
        {
            Operator = op;
            Operand = operand;
        }

        public ArtiTokenKind Operator { get; }
        public ArtiExpression Operand { get; }
    }

    public sealed class ArtiBinaryExpression : ArtiExpression
    {
        public ArtiBinaryExpression(ArtiSourceSpan span, ArtiExpression left, ArtiTokenKind op, ArtiExpression right)
            : base(span)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public ArtiExpression Left { get; }
        public ArtiTokenKind Operator { get; }
        public ArtiExpression Right { get; }
    }
}
