using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using dnGREP.Common;
using Newtonsoft.Json;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using DirectoryInfo = Alphaleonis.Win32.Filesystem.DirectoryInfo;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace dnGREP.WPF
{
    public class ScriptManager
    {
        public static readonly string ScriptExt = ".script";
        private static List<ScriptCommandDefinition> scriptCommands;
        private static List<ScriptingCompletionData> commandCompletionData;

        public static List<ScriptCommandDefinition> ScriptCommands
        {
            get
            {
                if (scriptCommands == null)
                {
                    LoadScriptCommands();
                }
                return scriptCommands;
            }
        }

        public static List<ScriptingCompletionData> CommandCompletionData
        {
            get
            {
                if (commandCompletionData == null)
                {
                    LoadScriptCommands();
                }
                return commandCompletionData;
            }
        }

        public static IEnumerable<string> TargetCommandNames => ScriptCommands.Where(sc => sc.IsTargetCommand)
                        .Select(sc => sc.Command);

        public static ScriptManager Instance { get; } = new ScriptManager();

        private ScriptManager()
        {
            LoadScripts();
        }

        private readonly IDictionary<string, string> _scripts = new Dictionary<string, string>();

        public ICollection<string> ScriptKeys { get { return _scripts.Keys; } }

        internal void LoadScripts()
        {
            _scripts.Clear();
            string dataFolder = Path.Combine(Utils.GetDataFolderPath(), "Scripts");
            foreach (string fileName in Directory.GetFiles(dataFolder, "*" + ScriptExt, SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string fileFolder = Path.GetDirectoryName(fileName);
                if (dataFolder != fileFolder)
                {
                    name = Path.GetRelativePath(dataFolder, fileFolder) + Path.DirectorySeparator + name;
                }
                if (!_scripts.ContainsKey(name))
                {
                    _scripts.Add(name, fileName);
                }
            }
        }

        private static void LoadScriptCommands()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "dnGREP.WPF.Scripting.ScriptCommands.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();

                scriptCommands = JsonConvert.DeserializeObject<List<ScriptCommandDefinition>>(
                    json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

                commandCompletionData = new List<ScriptingCompletionData>();

                foreach (var cmd in ScriptCommands)
                {
                    cmd.Initialize();

                    commandCompletionData.Add(new ScriptingCompletionData(cmd));
                }
            }
        }

        public Queue<ScriptStatement> ParseScript(string scriptKey)
        {
            Queue<ScriptStatement> statements = new Queue<ScriptStatement>();
            if (_scripts.TryGetValue(scriptKey, out string fileName) &&
                File.Exists(fileName))
            {
                int lineNum = 0;
                foreach (string line in File.ReadAllLines(fileName, Encoding.UTF8))
                {
                    lineNum++;

                    ScriptStatement statement = ParseLine(line, lineNum);

                    if (statement != null)
                    {
                        // special case to include another script:
                        if (statement.Command == "include" && statement.Target == "script")
                        {
                            string key = statement.Value;
                            if (!ScriptKeys.Contains(key) &&
                                key.EndsWith(ScriptExt, StringComparison.OrdinalIgnoreCase))
                            {
                                key = key.Remove(key.Length - ScriptExt.Length);
                            }

                            var inner = ParseScript(key);
                            while (inner.Count > 0)
                            {
                                statements.Enqueue(inner.Dequeue());
                            }
                        }
                        else
                        {
                            statements.Enqueue(statement);
                        }
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
                    bool isTargetRequired = !string.IsNullOrEmpty(command) && TargetCommandNames.Contains(command);
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
            var commandDef = ScriptCommands.FirstOrDefault(c => c.Command == statement.Command);

            if (commandDef == null)
            {
                error |= ScriptValidationError.InvalidCommand;
            }
            else if (commandDef.IsTargetCommand)
            {
                if (string.IsNullOrEmpty(statement.Target))
                {
                    error |= ScriptValidationError.RequiredTargetValueMissing;
                }
                else
                {
                    var targetDef = commandDef.Targets.FirstOrDefault(c => c.Target == statement.Target);
                    if (targetDef != null)
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(targetDef.ValueType);
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
                            if (!targetDef.AllowNullValue && statement.Value == null)
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
                }
            }
            else
            {
                switch (commandDef.Type)
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
                        else
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(commandDef.ValueType);
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
