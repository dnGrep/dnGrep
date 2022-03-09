using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace dnGREP.Common
{
    public class BooleanExpression
    {
        public ParserErrorState ParserState { get; private set; } = ParserErrorState.None;

        public string Expression { get; private set; }

        public string PostfixExpression { get; private set; }

        public List<BooleanToken> PostfixTokens { get; private set; }

        public IList<BooleanToken> Operands => PostfixTokens.Where(r => r.IsOperand).ToList();

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

        /// <summary>
        /// Tests if the expression will evaluate to true or false with any combination of remaining inputs
        /// </summary>
        /// <returns>null if indeterminate, otherwise the result of true or false
        public bool? ShortCircuitResult()
        {
            var savedState = Operands.Select(o => o.EvaluatedResult).ToList();

            List<List<bool>> values = new List<List<bool>>();
            for (int idx = 0; idx < Math.Pow(2, Operands.Count); idx++)
            {
                values.Add(new List<bool>());
                string binary = Convert.ToString(idx, 2).PadLeft(Operands.Count(), '0');

                for (int jdx = 0; jdx < Operands.Count; jdx++)
                {
                    var b = binary[jdx];
                    values[idx].Add(b != '0');
                }
            }

            bool? result = null;

            foreach (var row in values)
            {
                for (int col = 0; col < row.Count; col++)
                {
                    if (!Operands[col].EvaluatedResult.HasValue)
                    {
                        Operands[col].EvaluatedResult = row[col];
                    }
                }

                bool rowResult = EvaluateExpression();

                // restore original state
                for (int col = 0; col < Operands.Count; col++)
                {
                    Operands[col].EvaluatedResult = savedState[col];
                }

                if (result == null)
                {
                    result = rowResult;
                }
                else if (result != rowResult)
                {
                    return null;
                }
            }

            return result;
        }

        /// <summary>
        /// Tests if the expression will evaluate to true with all negative inputs
        /// </summary>
        /// <returns></returns>
        public bool IsNegativeExpression()
        {
            bool result = false;
            var savedState = Operands.Select(o => o.EvaluatedResult).ToList();

            foreach (var op in Operands)
            {
                op.EvaluatedResult = false;
            }

            result = EvaluateExpression();

            // restore original state
            for (int col = 0; col < Operands.Count; col++)
            {
                Operands[col].EvaluatedResult = savedState[col];
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
            ParserState = ParserErrorState.None;
            bool result = true;
            try
            {
                BooleanTokenizer tokenizer = new BooleanTokenizer();
                var list = tokenizer.Tokenize(input).ToList();

                result = BuildExpression(list);

                PostfixTokens = InfixToPostfix(list).ToList();
                PostfixExpression = string.Join(" ", PostfixTokens.Select(t => t.Value));

                int numUnaryOperators = PostfixTokens.Where(t => t.TokenType == TokenType.NOT).Count();
                int numBinaryOperators = PostfixTokens.Where(t => TokenType.Operator.HasFlag(t.TokenType)).Count() - numUnaryOperators;
                int numOperands = Operands.Count();

                if (numBinaryOperators != numOperands - 1)
                {
                    ParserState = ParserErrorState.InvalidExpression;
                    result = false;
                }
            }
            catch (InvalidStateException ex)
            {
                ParserState = ex.State;
                result = false;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                result = false;
                ParserState = ParserErrorState.UnknownError;
            }

            return result;
        }

        // the shunting-yard algorithm
        private IEnumerable<BooleanToken> InfixToPostfix(IEnumerable<BooleanToken> tokens)
        {
            if (tokens.Count(t => t.TokenType == TokenType.OpenParens) !=
                tokens.Count(t => t.TokenType == TokenType.CloseParens))
            {
                throw new InvalidStateException(ParserErrorState.MismatchedParentheses);
            }

            var stack = new Stack<BooleanToken>();
            TokenType previousToken = TokenType.NotDefined;
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
                        if (previousToken == TokenType.StringValue || previousToken == TokenType.CloseParens)
                        {
                            while (stack.Any() && token.IsLowerPrecedence(stack.Peek()))
                            {
                                yield return stack.Pop();
                            }
                            stack.Push(token);
                        }
                        else
                        {
                            throw new InvalidStateException(ParserErrorState.InvalidExpression);
                        }
                        break;

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
                        {
                            yield return stack.Pop();
                        }
                        stack.Pop();
                        break;

                    default:
                        throw new InvalidStateException(ParserErrorState.UnknownToken);
                }
                previousToken = token.TokenType;
            }
            while (stack.Any())
            {
                var token = stack.Pop();
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
            }

            Expression = sb.ToString().Replace("  ", " ").Trim();

            return true;
        }
    }

    public enum ParserErrorState
    {
        None = 0,
        UnknownToken,
        MismatchedParentheses,
        InvalidExpression,
        UnknownError,
    }

    public class InvalidStateException : Exception
    {
        public InvalidStateException(ParserErrorState state)
            : base(state.ToString())
        {
            State = state;
        }

        public ParserErrorState State { get; private set; }
    }
}
