namespace dnGREP.WPF
{
    public class ScriptValueDefinition
    {
        public string Value { get; set; } = string.Empty;

        public int Priority { get; set; } = int.MaxValue;

        public string Description { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{Priority} {Value}";
        }
    }
}