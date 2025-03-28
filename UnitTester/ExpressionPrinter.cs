using System.Collections;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace UnitTester
{
    public class ExpressionPrinter : ExpressionVisitor
    {
        public string ExpressionString { get; private set; }

        public string Print(Expression expression)
        {
            Visit(expression);
            return ExpressionString;
        }

        // Para expressões de comparação de igualdade (==, !=)
        protected override Expression VisitBinary(BinaryExpression node)
        {
            ExpressionString += $"({GetMemberNameOrValue(node.Left)} {TranslateExpressionType(node.NodeType)} {GetMemberNameOrValue(node.Right)})";
            return node;
        }

        public static string GetMemberNameOrValue(Expression expr)
        {
            if (expr is MemberExpression memberExpression)
            {
                if (memberExpression.Member.Name == "Length" && IsArrayOrCollection(memberExpression.Expression))
                {
                    return $"{memberExpression.Expression.Type.Name}.Length";
                }
                else if (memberExpression.Member.Name == "Count" && IsCollection(memberExpression.Expression.Type))
                {
                    return $"{memberExpression.Expression.Type.Name}.Count";
                }
                return memberExpression.Member.Name;
            }
            else if (expr is ConstantExpression constantExpression)
            {
                // Se for uma ConstantExpression, retorna o valor como string
                return constantExpression.Value?.ToString() ?? "null";
            }
            else if (expr is UnaryExpression unaryExpression)
            {
                // Para uma UnaryExpression, trata o operando
                return GetMemberNameOrValue(unaryExpression.Operand);
            }
            else if (expr is BinaryExpression binaryExpression)
            {
                // Para BinaryExpression, trata as expressões à esquerda e à direita
                string leftValue = GetMemberNameOrValue(binaryExpression.Left);
                string rightValue = GetMemberNameOrValue(binaryExpression.Right);
                return $"{leftValue} {TranslateExpressionType(expr.NodeType)} {rightValue}";
            }
            else
            {
                // Para outros tipos de expressões, simplesmente converta para string
                return expr.ToString();
            }
        }

        // Verifica se a expressão é um array ou coleção
        private static bool IsArrayOrCollection(Expression expr)
        {
            var type = expr.Type;
            return type.IsArray || IsCollection(type);
        }

        // Verifica se o tipo é uma coleção (ICollection ou IEnumerable)
        private static bool IsCollection(Type type)
        {
            return typeof(ICollection).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type);
        }


        // Trata membros (como 'a' ou 'a.Valor')
        protected override Expression VisitMember(MemberExpression node)
        {
            ExpressionString += node.Member.Name;
            return node;
        }

        // Trata constantes (como valores literais: 5, "teste", etc.)
        protected override Expression VisitConstant(ConstantExpression node)
        {
            ExpressionString += node.Value?.ToString() ?? "null";
            return node;
        }

        // Trata chamadas de método (como 'a.ToString()')
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            ExpressionString += $"{Visit(node.Object)}.{node.Method.Name}(";
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0)
                    ExpressionString += ", ";
                ExpressionString += Visit(node.Arguments[i]);
            }
            ExpressionString += ")";
            return node;
        }

        // Trata expressões unárias (como 'a != null' ou '!(a > b)')
        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    ExpressionString += $"!({Visit(node.Operand)})";
                    break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    // Trata a conversão explícita
                    ExpressionString += $"({Visit(node.Operand)})";
                    break;
                default:
                    ExpressionString += $"({Visit(node.Operand)})";
                    break;
            }
            return node;
        }


        // Para expressões de acesso a índices (como a[0])
        protected override Expression VisitIndex(IndexExpression node)
        {
            ExpressionString += $"{Visit(node.Object)}[";
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0)
                    ExpressionString += ", ";
                ExpressionString += Visit(node.Arguments[i]);
            }
            ExpressionString += "]";
            return node;
        }

        // Para expressões de lambda (como '(x => x > 0)')
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            ExpressionString += $"({Visit(node.Parameters[0])}) => {Visit(node.Body)}";
            return node;
        }

        // Para expressões de parâmetros (usadas em lógicas como lambda)
        protected override Expression VisitParameter(ParameterExpression node)
        {
            ExpressionString += node.Name;
            return node;
        }

        // Para expressões de conversão (como (int)a)
        protected override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            ExpressionString += $"({Visit(node.Expression)} is {node.Type})";
            return node;
        }

        // Para expressões de array ou coleção
        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            ExpressionString += "[";
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                if (i > 0)
                    ExpressionString += ", ";
                ExpressionString += Visit(node.Expressions[i]);
            }
            ExpressionString += "]";
            return node;
        }

        // Para expressões de instância de tipo (ex: 'new MyClass()')
        protected override Expression VisitNew(NewExpression node)
        {
            ExpressionString += $"new {node.Type.Name}(";
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                if (i > 0)
                    ExpressionString += ", ";
                ExpressionString += Visit(node.Arguments[i]);
            }
            ExpressionString += ")";
            return node;
        }



        public static string TranslateExpressionType(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add: return "+";
                case ExpressionType.AddAssign: return "+=";
                case ExpressionType.And: return "&";
                case ExpressionType.AndAlso: return "&&";
                case ExpressionType.ArrayIndex: return "[]"; // Para expressões de índice como a[0]
                case ExpressionType.ArrayLength: return ".Length"; // Acessando a propriedade Length de um array
                case ExpressionType.Call: return "Method Call"; // Chamada de método
                case ExpressionType.Coalesce: return "??";
                case ExpressionType.Equal: return "==";
                case ExpressionType.ExclusiveOr: return "^";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.Modulo: return "%";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.MultiplyAssign: return "*=";
                case ExpressionType.Negate: return "-";
                case ExpressionType.NegateChecked: return "-";
                case ExpressionType.New: return "new";
                case ExpressionType.NewArrayBounds: return "new[]";
                case ExpressionType.Not: return "!";
                case ExpressionType.NotEqual: return "!=";
                case ExpressionType.Or: return "|";
                case ExpressionType.OrElse: return "||";
                case ExpressionType.Subtract: return "-";
                case ExpressionType.SubtractAssign: return "-=";
                case ExpressionType.TypeAs: return "as";
                case ExpressionType.TypeIs: return "is";
                case ExpressionType.Divide: return "/";
                case ExpressionType.DivideAssign: return "/=";
                case ExpressionType.LeftShift: return "<<";
                case ExpressionType.RightShift: return ">>";
                case ExpressionType.Convert: return "(type)"; // Conversão explícita (type)
                case ExpressionType.ConvertChecked: return "(type)"; // Conversão checked explícita
                case ExpressionType.Assign: return "=";
                case ExpressionType.Throw: return "throw";
                case ExpressionType.Try: return "try";
                case ExpressionType.Decrement: return "--";
                case ExpressionType.Increment: return "++";
                case ExpressionType.IsFalse: return "is false";
                case ExpressionType.IsTrue: return "is true";
                case ExpressionType.Lambda: return "lambda expression";
                case ExpressionType.Loop: return "loop";
                case ExpressionType.Conditional: return "? :"; // Operador ternário
                case ExpressionType.MemberAccess: return "."; // Acesso a membros (propriedades, campos)
                case ExpressionType.Parameter: return "parameter";
                //case ExpressionType.CallMethod: return "method call";
                default: return type.ToString(); // Para tipos não tratados
            }
        }

    }
}
