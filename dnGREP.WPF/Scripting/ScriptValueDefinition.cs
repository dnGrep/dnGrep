namespace dnGREP.WPF
{
    public class ScriptValueDefinition
    {
        public string Value { get; set; }

        public int Priority { get; set; } = int.MaxValue;

        public string Description { get; set; } = null;

        public override string ToString()
        {
            return $"{Priority} {Value}";
        }
    }
}