using System;

namespace dnGREP.WPF
{
    public class ScriptStatement
    {
        public ScriptStatement(int line, string command, string target, string value)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("argument cannot be null or empty", nameof(command));
            }

            LineNumber = line;
            Command = command.ToLowerInvariant();
            Target = target?.ToLowerInvariant();
            Value = value;
        }

        public int LineNumber { get; private set; }

        public string Command { get; private set; }

        public string Target { get; private set; }

        public string Value { get; private set; }

        public override string ToString()
        {
            return $"{LineNumber}: {Command} T:{Target} V:{Value}";
        }
    }
}
