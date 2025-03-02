﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using dnGREP.Common;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    public partial class ScriptManager
    {
        public static readonly string ScriptFolder = "Scripts";
        public static readonly string ScriptExt = ".gsc";
        private static List<ScriptCommandDefinition>? scriptCommands;
        private static List<ScriptingCompletionData>? commandCompletionData;
        private readonly Dictionary<string, string> environmentVariables =
            new(StringComparer.OrdinalIgnoreCase);

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

        public IDictionary<string, string> ScriptEnvironmentVariables => environmentVariables;

        public void SetScriptEnvironmentVariable(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                environmentVariables.Remove(key);
            }
            else
            {
                environmentVariables[key] = value;
            }
        }

        public void ResetVariables()
        {
            ScriptEnvironmentVariables.Clear();
            ScriptEnvironmentVariables.Add("dnGrep_logDir", DirectoryConfiguration.Instance.LogDirectory);
            ScriptEnvironmentVariables.Add("dnGrep_dataDir", DirectoryConfiguration.Instance.DataDirectory);
            ScriptEnvironmentVariables.Add("dnGrep_scriptDir", Path.Combine(DirectoryConfiguration.Instance.DataDirectory, ScriptFolder));
        }

        [GeneratedRegex("%(\\w+?)%")]
        private static partial Regex EnvVariableRegex();

        public string ExpandEnvironmentVariables(string text)
        {
            if (!string.IsNullOrEmpty(text) && text.Contains('%', StringComparison.Ordinal))
            {
                string result = EnvVariableRegex().Replace(text, m =>
                {
                    if (ScriptEnvironmentVariables.TryGetValue(m.Groups[1].Value, out string? value))
                    {
                        return value;
                    }
                    return m.Value;
                });

                result = Environment.ExpandEnvironmentVariables(result);

                return result;
            }
            return text;
        }

        public static IEnumerable<string> TargetCommandNames => ScriptCommands.Where(sc => sc.IsTargetCommand)
                        .Select(sc => sc.Command);

        public static ScriptManager Instance { get; } = new ScriptManager();

        private ScriptManager()
        {
            ResetVariables();
        }

        private readonly Dictionary<string, string> _scripts = [];

        public ICollection<string> ScriptKeys { get { return _scripts.Keys; } }

        private static readonly char[] space = [' '];

        public string GetScriptPath(string script)
        {
            if (_scripts.TryGetValue(script, out var path))
            {
                return Path.GetDirectoryName(path) ?? string.Empty;
            }
            return Path.GetDirectoryName(script) ?? string.Empty;
        }

        internal void LoadScripts()
        {
            _scripts.Clear();
            string dataFolder = Path.Combine(DirectoryConfiguration.Instance.DataDirectory, ScriptFolder);
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }

            foreach (string fileName in Directory.GetFiles(dataFolder, "*" + ScriptExt, SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string? fileFolder = Path.GetDirectoryName(fileName);
                if (fileFolder != null && dataFolder != fileFolder)
                {
                    name = Path.GetRelativePath(dataFolder, fileFolder) + Path.DirectorySeparatorChar + name;
                }
                _scripts.TryAdd(name, fileName);
            }
        }

        internal static List<string> GetScriptNames()
        {
            List<string> results = [];
            string dataFolder = Path.Combine(DirectoryConfiguration.Instance.DataDirectory, ScriptFolder);
            if (Directory.Exists(dataFolder))
            {
                foreach (string fileName in Directory.GetFiles(dataFolder, "*" + ScriptExt, SearchOption.AllDirectories))
                {
                    string name = Path.GetFileNameWithoutExtension(fileName);
                    string? fileFolder = Path.GetDirectoryName(fileName);
                    if (fileFolder != null && dataFolder != fileFolder)
                    {
                        name = Path.GetRelativePath(dataFolder, fileFolder) + '_' + name;
                    }
                    results.Add(name.Replace(Path.DirectorySeparatorChar, '_'));
                }
            }
            return results;
        }

        [MemberNotNull(nameof(scriptCommands), nameof(commandCompletionData))]
        private static void LoadScriptCommands()
        {
            scriptCommands = [];
            commandCompletionData = [];

            ScriptCommandInitializer.LoadScriptCommands(ref scriptCommands, ref commandCompletionData);
        }

        public Queue<ScriptStatement> ParseScript(string scriptKey, bool recursive = true)
        {
            if (_scripts.TryGetValue(scriptKey, out string? fileName) && File.Exists(fileName))
            {
                return ParseScript(File.ReadAllLines(fileName, Encoding.UTF8), recursive);
            }

            return new Queue<ScriptStatement>();
        }

        public Queue<ScriptStatement> ParseScript(IEnumerable<string> scriptText, bool recursive)
        {
            Queue<ScriptStatement> statements = new();

            int lineNum = 0;
            foreach (string line in scriptText)
            {
                lineNum++;

                ScriptStatement? statement = ParseLine(line, lineNum);

                if (statement != null)
                {
                    // special case to include another script:
                    if (recursive && statement.Command == "include" && statement.Target == "script")
                    {
                        if (statement.Value != null)
                        {
                            string key = statement.Value;
                            if (!ScriptKeys.Contains(key) &&
                                key.EndsWith(ScriptExt, StringComparison.OrdinalIgnoreCase))
                            {
                                key = key.Remove(key.Length - ScriptExt.Length);
                            }

                            var inner = ParseScript(key, recursive);
                            while (inner.Count > 0)
                            {
                                statements.Enqueue(inner.Dequeue());
                            }
                        }
                    }
                    else
                    {
                        statements.Enqueue(statement);
                    }
                }
            }
            return statements;
        }

        public static ScriptStatement? ParseLine(string line, int lineNum)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                return null;
            }

            string command, target = string.Empty, value = string.Empty;

            string[] parts = line.Split(space, StringSplitOptions.RemoveEmptyEntries);

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
                            int pos = line.IndexOf(target, StringComparison.Ordinal);
                            if (pos > -1 && pos + target.Length + 1 < line.Length)
                            {
                                if (target.Equals("folder", StringComparison.Ordinal))
                                {
                                    // keep quotes
                                    value = line[(pos + target.Length + 1)..].Trim();
                                }
                                else
                                {
                                    value = Trim(line[(pos + target.Length + 1)..]);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (command.Length + 1 < line.Length)
                        {
                            value = Trim(line[(command.Length + 1)..]);
                        }
                    }
                }

                return new ScriptStatement(lineNum, command, target, value);
            }

            return null;
        }

        private static string Trim(string value)
        {
            string result = value.TrimStart();

            if (result.StartsWith('"') && result.TrimEnd().EndsWith('"'))
            {
                result = result.TrimEnd();
                result = result[1..^1];
            }
            else
            {
                result = result.TrimEnd();
            }

            return result;
        }

        public List<Tuple<int, ScriptValidationError>> Validate(Queue<ScriptStatement> statements)
        {
            List<Tuple<int, ScriptValidationError>> errors = [];

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

        public Tuple<int, ScriptValidationError>? Validate(ScriptStatement? statement)
        {
            if (statement == null)
            {
                return new(0, ScriptValidationError.NullStatement);
            }

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
                    if (targetDef != null && targetDef.ValueType != null)
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
                            if (string.IsNullOrEmpty(statement.Value))
                            {
                                if (!targetDef.AllowNullValue)
                                {
                                    error |= ScriptValidationError.NullValueNotAllowed;
                                }
                            }
                            else
                            {
                                try
                                {
                                    object? obj = converter.ConvertFromString(statement.Value);
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

                        if (statement.Command == "include" && statement.Target == "script")
                        {
                            string key = statement.Value ?? string.Empty;
                            if (!ScriptKeys.Contains(key) &&
                                key.EndsWith(ScriptExt, StringComparison.OrdinalIgnoreCase))
                            {
                                key = key.Remove(key.Length - ScriptExt.Length);
                            }

                            if (!ScriptKeys.Contains(key))
                            {
                                error |= ScriptValidationError.IncludeScriptKeyNotFound;
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
                switch (commandDef.ValueType?.Name)
                {
                    case null:
                        if (!string.IsNullOrEmpty(statement.Value))
                        {
                            error |= ScriptValidationError.UnneededValueFound;
                        }
                        break;

                    case "String":
                        if (string.IsNullOrEmpty(statement.Value))
                        {
                            error |= ScriptValidationError.RequiredStringValueMissing;
                        }
                        break;

                    case "Boolean":
                        if (string.IsNullOrEmpty(statement.Value))
                        {
                            error |= ScriptValidationError.RequiredBooleanValueMissing;
                        }
                        else
                        {
                            TypeConverter converter = TypeDescriptor.GetConverter(commandDef.ValueType);
                            try
                            {
                                object? obj = converter.ConvertFromString(statement.Value);
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

        public static string ToErrorString(ScriptValidationError value)
        {
            string separator = CultureInfo.CurrentCulture.TextInfo.ListSeparator + " ";

            string text = string.Empty;
            if (value.HasFlag(ScriptValidationError.InvalidCommand))
            {
                text += Resources.Script_Validation_TheCommandIsInvalid + separator;
            }
            if (value.HasFlag(ScriptValidationError.RequiredTargetValueMissing))
            {
                text += Resources.Script_Validation_ThisCommandRequiresATargetParameter + separator;
            }
            if (value.HasFlag(ScriptValidationError.InvalidTargetName))
            {
                text += Resources.Script_Validation_TheTargetNameIsInvalid + separator;
            }
            if (value.HasFlag(ScriptValidationError.UnneededValueFound))
            {
                text += Resources.Script_Validation_ThisCommandDoesNotNeedAValueParameter + separator;
            }
            if (value.HasFlag(ScriptValidationError.RequiredStringValueMissing))
            {
                text += Resources.Script_Validation_ThisCommandRequiresAStringValue + separator;
            }
            if (value.HasFlag(ScriptValidationError.RequiredBooleanValueMissing))
            {
                text += Resources.Script_Validation_ThisCommandRequiresABooleanValue + separator;
            }
            if (value.HasFlag(ScriptValidationError.CannotConvertValueFromString))
            {
                text += Resources.Script_Validation_TheValueCannotBeConvertedFromTheString + separator;
            }
            if (value.HasFlag(ScriptValidationError.NullValueNotAllowed))
            {
                text += Resources.Script_Validation_ThisCommandDoesNotAllowANullValue + separator;
            }
            if (value.HasFlag(ScriptValidationError.ConvertValueFromStringFailed))
            {
                text += Resources.Script_Validation_TheValueCouldNotBeConvertedToTheCorrectType + separator;
            }
            if (value.HasFlag(ScriptValidationError.IncludeScriptKeyNotFound))
            {
                text += Resources.Script_Validation_TheScriptKeyWasNotFound + separator;
            }

            return text.TrimEnd(separator.ToCharArray());
        }
    }


    [Flags]
    public enum ScriptValidationError
    {
        None = 0,
        NullStatement = 1,
        InvalidCommand = 2,
        RequiredTargetValueMissing = 4,
        InvalidTargetName = 8,
        UnneededValueFound = 16,
        RequiredStringValueMissing = 32,
        RequiredBooleanValueMissing = 64,
        CannotConvertValueFromString = 128,
        NullValueNotAllowed = 256,
        ConvertValueFromStringFailed = 512,
        IncludeScriptKeyNotFound = 1024,
    }
}
