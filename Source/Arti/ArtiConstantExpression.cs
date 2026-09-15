using System;
using System.Linq;

namespace AdvancedRimTalk.Arti
{
    internal static class ArtiConstantExpression
    {
        internal static bool IsStatic(ArtiExpression expression, Func<string, bool> isConstant)
        {
            if (expression is ArtiLiteralExpression) return true;
            if (expression is ArtiNameExpression name) return isConstant(name.Name);
            if (expression is ArtiUnaryExpression unary) return IsStatic(unary.Operand, isConstant);
            if (expression is ArtiBinaryExpression binary)
                return IsStatic(binary.Left, isConstant) && IsStatic(binary.Right, isConstant);
            if (expression is ArtiArrayExpression array) return array.Items.All(item => IsStatic(item, isConstant));
            if (expression is ArtiObjectExpression obj) return obj.Members.All(member => IsStatic(member.Value, isConstant));
            if (expression is ArtiIndexExpression index)
                return IsStatic(index.Target, isConstant) && IsStatic(index.Index, isConstant);
            if (expression is ArtiMemberExpression memberExpression) return IsStatic(memberExpression.Target, isConstant);
            return false;
        }
    }
}
