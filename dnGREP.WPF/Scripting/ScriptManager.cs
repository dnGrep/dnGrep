using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class ScriptManager
    {
        public static ICollection<string> CommandNames => commands.Keys;
        public static ICollection<string> TargetCommandNames => targetCommands;

        private static readonly IDictionary<string, string> commands = new Dictionary<string, string>
        {
            {"set",               "set"},
            {"use",               "use"},
            {"add",               "add"},
            {"remove",            "remove"},
            {"report",            "report"},
            {"reset",             null},
            {"sort",              null},
            {"undo",              null},
            {"copyfiles",         "string"},
            {"movefiles",         "string"},
            {"deletefiles",       null},
            {"copyfilenames",     null},
            {"copyresults",       null},
            {"showfileoptions",   "bool"},
            {"maximizeresults",   "bool"},
            {"search",            null},
            {"replace",           null},
            {"messages",          null},
            {"exit",              null},
        };

        private static readonly List<string> targetCommands = new List<string>
        {
            "set",
            "add",
            "remove",
            "use",
            "report"
        };

        public static ScriptManager Instance { get; } = new ScriptManager();

        private ScriptManager()
        {
            LoadScripts();
        }

        private readonly IDictionary<string, string> _scripts = new Dictionary<string, string>();

        public ICollection<string> Scripts { get { return _scripts.Keys; } }

        private void LoadScripts()
        {
            string dataFolder = Utils.GetDataFolderPath();
            foreach (string fileName in Directory.GetFiles(dataFolder, "*.script", SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                if (!_scripts.ContainsKey(name))
                {
                    _scripts.Add(name, fileName);
                }
            }
        }

        public Queue<ScriptStatement> ParseScript(string scriptName)
        {
            Queue<ScriptStatement> statements = new Queue<ScriptStatement>();
            if (_scripts.TryGetValue(scriptName, out string fileName) &&
                File.Exists(fileName))
            {
                int lineNum = 0;
                foreach (string line in File.ReadAllLines(fileName, Encoding.UTF8))
                {
                    lineNum++;

                    ScriptStatement statement = ParseLine(line, lineNum);

                    if (statement != null)
                    {
                        statements.Enqueue(statement);
                    }
                }
            }
            return statements;
        }

        public ScriptStatement ParseLine(string line, int lineNum)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
            {
                return null;
            }

            string command, target = null, value = null;

            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                command = parts[0].Trim();

                if (parts.Length > 1)
                {
                    bool isTargetRequired = !string.IsNullOrEmpty(command) && targetCommands.Contains(command);
                    if (isTargetRequired)
                    {
                        target = parts[1].Trim();

                        if (parts.Length > 2)
                        {
                            int pos = line.IndexOf(target);
                            if (pos > -1 && pos + target.Length + 1 < line.Length)
                            {
                                value = Trim(line.Substring(pos + target.Length + 1));
                            }
                        }
                    }
                    else
                    {
                        if (command.Length + 1 < line.Length)
                        {
                            value = Trim(line.Substring(command.Length + 1));
                        }
                    }
                }

                return new ScriptStatement(lineNum, command, target, value);
            }

            return null;
        }

        private string Trim(string value)
        {
            string result = value.TrimStart();

            if (result.StartsWith("\"") && result.TrimEnd().EndsWith("\""))
            {
                result = result.TrimEnd();
                result = result.Substring(1, result.Length - 2);
            }
            else
            {
                result = result.TrimEnd();
            }

            return result;
        }

        public static List<Tuple<int, ScriptValidationError>> Validate(Queue<ScriptStatement> statements)
        {
            List<Tuple<int, ScriptValidationError>> errors = new List<Tuple<int, ScriptValidationError>>();

            foreach (ScriptStatement statement in statements)
            {
                var error = Validate(statement);
                if (error != null)
                {
                    errors.Add(error);
                }
            }
            return errors;
        }

        public static Tuple<int, ScriptValidationError> Validate(ScriptStatement statement)
        {
            ScriptValidationError error = ScriptValidationError.None;

            if (!commands.Keys.Contains(statement.Command))
            {
                error |= ScriptValidationError.InvalidCommand;
            }
            else
            {
                string argType = commands[statement.Command];
                IDictionary<string, IScriptCommand> commandMap = null;

                switch (argType)
                {
                    case "set":
                        commandMap = MainViewModel.SetCommandMap;
                        break;
                    case "use":
                        commandMap = MainViewModel.UseCommandMap;
                        break;
                    case "add":
                        commandMap = MainViewModel.AddCommandMap;
                        break;
                    case "remove":
                        commandMap = MainViewModel.RemoveCommandMap;
                        break;
                    case "report":
                        commandMap = MainViewModel.ReportCommandMap;
                        break;
                }

                switch (argType)
                {
                    case null:
                        if (!string.IsNullOrEmpty(statement.Value))
                        {
                            error |= ScriptValidationError.UnneededValueFound;
                        }
                        break;

                    case "string":
                        if (string.IsNullOrEmpty(statement.Value))
                        {
                            error |= ScriptValidationError.RequiredStringValueMissing;
                        }
                        break;

                    case "bool":
                        if (string.IsNullOrEmpty(statement.Value))
                        {
                            error |= ScriptValidationError.RequiredBooleanValueMissing;
                        }
                        break;

                    case "set":
                    case "use":
                    case "add":
                    case "remove":
                    case "report":
                        if (string.IsNullOrEmpty(statement.Target))
                        {
                            error |= ScriptValidationError.RequiredTargetValueMissing;
                        }
                        else if (commandMap.TryGetValue(statement.Target, out IScriptCommand command))
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(command.ValueType);
                            if (converter == null)
                            {
                                error |= ScriptValidationError.CannotConvertValueFromString;
                            }
                            else if (!converter.CanConvertFrom(typeof(string)))
                            {
                                error |= ScriptValidationError.CannotConvertValueFromString;
                            }
                            else
                            {
                                if (!command.AllowNullValue && statement.Value == null)
                                {
                                    error |= ScriptValidationError.NullValueNotAllowed;
                                }
                                else
                                {
                                    try
                                    {
                                        object obj = converter.ConvertFromString(statement.Value);
                                        if (obj == null)
                                        {
                                            error |= ScriptValidationError.ConvertValueFromStringFailed;
                                        }
                                    }
                                    catch
                                    {
                                        error |= ScriptValidationError.ConvertValueFromStringFailed;
                                    }
                                }
                            }
                        }
                        else
                        {
                            error |= ScriptValidationError.InvalidTargetName;
                        }
                        break;
                }
            }

            if (error != ScriptValidationError.None)
            {
                return new Tuple<int, ScriptValidationError>(statement.LineNumber, error);
            }
            else
            {
                return null;
            }
        }
    }


    [Flags]
    public enum ScriptValidationError
    {
        None = 0,
        InvalidCommand = 1,
        RequiredTargetValueMissing = 2,
        InvalidTargetName = 4,
        UnneededValueFound = 8,
        RequiredStringValueMissing = 16,
        RequiredBooleanValueMissing = 32,
        CannotConvertValueFromString = 64,
        NullValueNotAllowed = 128,
        ConvertValueFromStringFailed = 256,
    }
}
