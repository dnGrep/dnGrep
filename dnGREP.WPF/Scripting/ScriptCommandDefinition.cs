using System;
using System.Collections.Generic;

namespace dnGREP.WPF
{
    public class ScriptCommandDefinition
    {
        public string Command { get; set; }

        public int Priority { get; set; } = int.MaxValue;

        public string Description { get; set; } = null;

        public string ValueHint { get; set; } = null;

        public Type ValueType { get; set; } = null;

        public bool AllowNullValue { get; set; } = false;

        public List<ScriptTargetDefinition> Targets { get; } = new List<ScriptTargetDefinition>();

        public List<ScriptValueDefinition> Values { get; } = new List<ScriptValueDefinition>();

        public List<ScriptingCompletionData> CompletionData { get; } = new List<ScriptingCompletionData>();

        public bool IsTargetCommand => Targets.Count > 0;

        public void Initialize()
        {
            if (ValueType == typeof(bool))
            {
                Values.Add(new ScriptValueDefinition { Priority = 1, Value = "False" });
                Values.Add(new ScriptValueDefinition { Priority = 0, Value = "True" });
            }

            Targets.Sort((x, y) => x.Target.CompareTo(y.Target));

            foreach (var target in Targets)
            {
                target.Initialize();

                CompletionData.Add(new ScriptingCompletionData(target));
            }

            foreach (var value in Values)
            {
                CompletionData.Add(new ScriptingCompletionData(value));
            }
        }

        public override string ToString()
        {
            return $"{Priority} {Command} T:{Targets.Count} V:{Values.Count} {ValueType?.Name}";
        }
    }
}
