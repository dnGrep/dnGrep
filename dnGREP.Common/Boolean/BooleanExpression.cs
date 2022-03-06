using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace dnGREP.Common
{
    public class BooleanExpression
    {
        public string Expression { get; private set; }

        public string PostfixExpression { get; private set; }

        public List<BooleanToken> PostfixTokens { get; private set; }

        public IEnumerable<BooleanToken> Operands => PostfixTokens.Where(r => r.IsOperand);

        public bool? Evaluate()
        {
            bool? result = null;

            try
            {
                bool isComplete = Operands.All(o => o.EvaluatedResult.HasValue);

                if (isComplete)
                {
                    result = EvaluateExpression();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error evaluating expression: " + ex.Message);
            }

            return result;
        }

        private bool EvaluateExpression()
        {
            Stack<bool> operandStack = new Stack<bool>();

            foreach (var token in PostfixTokens)
            {
                switch (token.TokenType)
                {
                    case TokenType.StringValue:
                        if (token.EvaluatedResult.HasValue)
                        {
                            operandStack.Push(token.EvaluatedResult.Value);
                        }
                        else
                        {
                            throw new Exception("Expression is incomplete");
                        }
                        break;
                    case TokenType.NOT:
                        {
                            bool a = operandStack.Pop();
                            operandStack.Push(!a);
                        }
                        break;
                    case TokenType.AND:
                        {
                            bool b = operandStack.Pop();
                            bool a = operandStack.Pop();
                            operandStack.Push(a && b);
                        }
                        break;
                    case TokenType.NAND:
                        {
                            bool b = operandStack.Pop();
                            bool a = operandStack.Pop();
                            operandStack.Push(!(a && b));
                        }
                        break;
                    case TokenType.OR:
                        {
                            bool b = operandStack.Pop();
                            bool a = operandStack.Pop();
                            operandStack.Push(a || b);
                        }
                        break;
                    case TokenType.NOR:
                        {
                            bool b = operandStack.Pop();
                            bool a = operandStack.Pop();
                            operandStack.Push(!(a || b));
                        }
                        break;
                }
            }
            return operandStack.Pop();
        }

        public bool TryParse(string input)
        {
            bool result = true;
            try
            {
                BooleanTokenizer tokenizer = new BooleanTokenizer();
                var list = tokenizer.Tokenize(input).ToList();

                result = BuildExpression(list);

                PostfixTokens = InfixToPostfix(list).ToList();
                PostfixExpression = string.Join(" ", PostfixTokens.Select(t => t.Value));
            }
            catch (Exception ex)
            {
                result = false;
                string message = ex.Message;
            }

            return result;
        }

        // the shunting-yard algorithm
        private IEnumerable<BooleanToken> InfixToPostfix(IEnumerable<BooleanToken> tokens)
        {
            var stack = new Stack<BooleanToken>();
            foreach (var token in tokens)
            {
                switch (token.TokenType)
                {
                    case TokenType.StringValue:
                        yield return token;
                        break;
                    case TokenType.AND:
                    case TokenType.NAND:
                    case TokenType.OR:
                    case TokenType.NOR:
                    case TokenType.NOT:
                        while (stack.Any() && token.IsLowerPrecedence(stack.Peek()))
                        {
                            yield return stack.Pop();
                        }
                        stack.Push(token);
                        break;
                    case TokenType.OpenParens:
                        stack.Push(token);
                        break;
                    case TokenType.CloseParens:
                        while (stack.Peek().TokenType != TokenType.OpenParens)
                            yield return stack.Pop();
                        stack.Pop();
                        break;
                    default:
                        throw new Exception("Unknown token");
                }
            }
            while (stack.Any())
            {
                var token = stack.Pop();
                if (token.IsParenthesis)
                    throw new Exception("Mismatched parentheses");
                yield return token;
            }
        }

        private bool BuildExpression(IEnumerable<BooleanToken> tokens)
        {
            StringBuilder sb = new StringBuilder();
            char key = 'a';
            foreach (BooleanToken token in tokens)
            {
                if (token.TokenType == TokenType.StringValue)
                {
                    if (!string.IsNullOrWhiteSpace(token.Value))
                    {
                        sb.Append(key.ToString());
                        key++;
                    }
                }
                else if (token.TokenType == TokenType.OpenParens)
                {
                    sb.Append(" ( ");
                }
                else if (token.TokenType == TokenType.CloseParens)
                {
                    sb.Append(" ) ");
                }
                else if (TokenType.Operator.HasFlag(token.TokenType))
                {
                    if (token.Value.StartsWith(")"))
                    {
                        sb.Append(" ) ");
                    }
                    sb.Append($" {token.TokenType} ");
                    if (token.Value.EndsWith("("))
                    {
                        sb.Append(" ( ");
                    }
                }
                //else if (token.TokenType == TokenType.AND)
                //{
                //    if (token.Value.StartsWith(")"))
                //    {
                //        sb.Append(" ) ");
                //    }
                //    sb.Append(" AND ");
                //    if (token.Value.EndsWith("("))
                //    {
                //        sb.Append(" ( ");
                //    }
                //}
                //else if (token.TokenType == TokenType.OR)
                //{
                //    if (token.Value.StartsWith(")"))
                //    {
                //        sb.Append(" ) ");
                //    }
                //    sb.Append(" OR ");
                //    if (token.Value.EndsWith("("))
                //    {
                //        sb.Append(" ( ");
                //    }
                //}
                //else if (token.TokenType == TokenType.NOT)
                //{
                //    if (token.Value.StartsWith(")"))
                //    {
                //        sb.Append(" ) ");
                //    }
                //    sb.Append(" NOT ");
                //    if (token.Value.EndsWith("("))
                //    {
                //        sb.Append(" ( ");
                //    }
                //}
            }

            Expression = sb.ToString().Replace("  ", " ").Trim();

            return true;
        }
    }
}
