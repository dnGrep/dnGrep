﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization.Properties;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public partial class ScriptViewModel : CultureAwareViewModel
    {
        private readonly TextEditor textEditor;
        private string originalScript = string.Empty;
        private static bool beenInitialized;

        public event EventHandler? RequestRun;
        public event EventHandler? RequestClose;
        public event EventHandler? RequestSuggest;
        public event EventHandler? NewScriptFileSaved;

        static ScriptViewModel()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (beenInitialized) return;

            beenInitialized = true;
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(NewCommand), "Script_Editor_New", "Control+N");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(SaveCommand), "Script_Editor_Save", "Control+S");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(SaveAsCommand), "Script_Editor_SaveAs", "Control+Shift+S");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(CloseCommand), "Script_Editor_Close", "Control+W");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(UndoCommand), "Script_Editor_Undo", "Control+Z");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(RedoCommand), "Script_Editor_Redo", "Control+Y");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(CutCommand), "Script_Editor_Cut", "Control+X");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(CopyCommand), "Script_Editor_Copy", "Control+C");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(PasteCommand), "Script_Editor_Paste", "Control+V");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(DeleteCommand), "Script_Editor_Delete", "Delete");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(DeleteLineCommand), "Script_Editor_DeleteLine", "Control+D");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(MoveLinesUpCommand), "Script_Editor_MoveUp", "Alt+Up");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(MoveLinesDownCommand), "Script_Editor_MoveDown", "Alt+Down");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(SuggestCommand), "Script_Editor_Suggest", "Control+Space");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(ValidateCommand), "Script_Editor_Validate", "Control+L");
            KeyBindingManager.RegisterCommand(KeyCategory.Script, nameof(RunCommand), "Script_Editor_RunScript", "Control+R");
        }

        public ScriptViewModel(TextEditor textEditor)
        {
            this.textEditor = textEditor;

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            ResultsFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsFontSize);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            textEditor.Document.TextChanged += Document_TextChanged;

            InitializeInputBindings();
            App.Messenger.Register<KeyCategory>("KeyGestureChanged", OnKeyGestureChanged);
        }

        private void InitializeInputBindings()
        {
            foreach (KeyBindingInfo kbi in KeyBindingManager.GetCommandGestures(KeyCategory.Script))
            {
                PropertyInfo? pi = GetType().GetProperty(kbi.CommandName, BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.GetValue(this) is RelayCommand cmd)
                {
                    InputBindings.Add(KeyBindingManager.CreateKeyBinding(cmd, kbi.KeyGesture));
                }
            }
        }

        private void OnKeyGestureChanged(KeyCategory category)
        {
            if (category == KeyCategory.Script)
            {
                InputBindings.Clear();
                InitializeInputBindings();
                InputBindings.RaiseAfterCollectionChanged();
            }
        }

        public ObservableCollectionEx<InputBinding> InputBindings { get; } = [];


        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double mainFormFontSize;

        [ObservableProperty]
        private string resultsFontFamily = GrepSettings.DefaultMonospaceFontFamily;

        [ObservableProperty]
        private double resultsFontSize;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private string windowTitle = Resources.Script_Editor_Title;

        [ObservableProperty]
        private string scriptFile = string.Empty;

        [ObservableProperty]
        private bool isModified = false;

        [ObservableProperty]
        private bool hasValidationErrors = false;

        public ObservableCollection<ValidationErrorViewModel> ValidationData { get; } = [];

        private RelayCommand? newCommand;
        public RelayCommand NewCommand => newCommand ??= new RelayCommand(
            p => NewScript(),
            q => true);

        private RelayCommand? saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(
            p => Save(),
            q => true);

        private RelayCommand? saveAsCommand;
        public RelayCommand SaveAsCommand => saveAsCommand ??= new RelayCommand(
            p => SaveAs(true),
            q => true);

        private RelayCommand? validateCommand;
        public RelayCommand ValidateCommand => validateCommand ??= new RelayCommand(
            p => ValidateScript(false),
            q => true);

        private RelayCommand? closeCommand;
        public RelayCommand CloseCommand => closeCommand ??= new RelayCommand(
            p => Close(),
            q => true);

        private RelayCommand? helpCommand;
        public RelayCommand HelpCommand => helpCommand ??= new RelayCommand(
            p =>
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = @"https://github.com/dnGrep/dnGrep/wiki/Scripting%20Commands",
                    UseShellExecute = true,
                };
                using var proc = Process.Start(startInfo);
            });

        private RelayCommand? undoCommand;
        public RelayCommand UndoCommand => undoCommand ??= new RelayCommand(
            p => textEditor.Undo(),
            q => textEditor.CanUndo);

        private RelayCommand? redoCommand;
        public RelayCommand RedoCommand => redoCommand ??= new RelayCommand(
            p => textEditor.Redo(),
            q => textEditor.CanRedo);

        private RelayCommand? cutCommand;
        public RelayCommand CutCommand => cutCommand ??= new RelayCommand(
            p => textEditor.Cut(),
            q => CanCutOrCopy);

        private RelayCommand? copyCommand;
        public RelayCommand CopyCommand => copyCommand ??= new RelayCommand(
            p => textEditor.Copy(),
            q => CanCutOrCopy);

        private RelayCommand? pasteCommand;
        public RelayCommand PasteCommand => pasteCommand ??= new RelayCommand(
            p => textEditor.Paste(),
            q => CanPaste);

        private RelayCommand? deleteCommand;
        public RelayCommand DeleteCommand => deleteCommand ??= new RelayCommand(
            p => textEditor.Delete(),
            q => textEditor.TextArea != null && textEditor.TextArea.Document != null);

        private RelayCommand? deleteLineCommand;
        public RelayCommand DeleteLineCommand => deleteLineCommand ??= new RelayCommand(
            p => AvalonEditCommands.DeleteLine.Execute(null, textEditor.TextArea),
            q => textEditor.TextArea != null && textEditor.TextArea.Document != null);

        private bool CanCutOrCopy =>
            textEditor.TextArea != null && textEditor.TextArea.Document != null &&
            (textEditor.TextArea.Options.CutCopyWholeLine || !textEditor.TextArea.Selection.IsEmpty);

        private bool CanPaste =>
            textEditor.TextArea != null && textEditor.TextArea.Document != null &&
            textEditor.TextArea.ReadOnlySectionProvider.CanInsert(textEditor.TextArea.Caret.Offset)
                    && Clipboard.ContainsText();

        private RelayCommand? suggestCommand;
        public RelayCommand SuggestCommand => suggestCommand ??= new RelayCommand(
            p => RequestSuggest?.Invoke(this, EventArgs.Empty),
            q => true);

        private RelayCommand? moveLinesUpCommand;
        public RelayCommand MoveLinesUpCommand => moveLinesUpCommand ??= new RelayCommand(
            p => MoveSelectedLinesUp(),
            q => CanMoveLineUp);

        private RelayCommand? moveLinesDownCommand;
        public RelayCommand MoveLinesDownCommand => moveLinesDownCommand ??= new RelayCommand(
            p => MoveSelectedLinesDown(),
            q => CanMoveLineDown);

        private RelayCommand? runCommand;
        public RelayCommand RunCommand => runCommand ??= new RelayCommand(
            p => RunScript(),
            q => true);

        private bool CanMoveLineUp
        {
            get
            {
                if (textEditor.TextArea != null && textEditor.TextArea.Document != null)
                {
                    var beginline = textEditor.Document.GetLineByOffset(textEditor.SelectionStart);
                    return beginline.PreviousLine != null;
                }
                return false;
            }
        }

        private bool CanMoveLineDown
        {
            get
            {
                if (textEditor.TextArea != null && textEditor.TextArea.Document != null)
                {
                    int adj = textEditor.TextArea.Caret.VisualColumn == 0 && textEditor.SelectionLength > 0 ? -1 : 0;
                    var finalline = textEditor.Document.GetLineByOffset(textEditor.SelectionStart + textEditor.SelectionLength + adj);
                    return finalline.NextLine != null;
                }
                return false;
            }
        }

        private void MoveSelectedLinesUp()
        {
            if (CanMoveLineUp)
            {
                textEditor.Document.BeginUpdate();

                var selectionStart = textEditor.SelectionStart;
                var selectionLength = textEditor.SelectionLength;

                // check for caret in the first column of the line following the selection
                // if so, don't include that line
                int adj = textEditor.TextArea.Caret.VisualColumn == 0 && selectionLength > 0 ? -1 : 0;

                var beginLine = textEditor.Document.GetLineByOffset(selectionStart);
                var finalLine = textEditor.Document.GetLineByOffset(selectionStart + selectionLength + adj);
                var length = finalLine.EndOffset - beginLine.Offset;
                var prevLine = beginLine.PreviousLine;

                var newCaretOffset = textEditor.CaretOffset - prevLine.Length - prevLine.DelimiterLength;
                var newSelectionStart = selectionStart - prevLine.Length - prevLine.DelimiterLength;

                var linesToMove = textEditor.Document.GetText(beginLine.Offset, length + finalLine.DelimiterLength);

                // is finalLine the last line in the document? it won't have a newline
                bool removeLastDelimiter = false;
                if (finalLine.DelimiterLength == 0)
                {
                    linesToMove += Environment.NewLine;
                    removeLastDelimiter = true;
                }

                textEditor.Document.Remove(beginLine.Offset, length + finalLine.DelimiterLength);
                textEditor.Document.Insert(prevLine.Offset, linesToMove);

                // move the caret to the same position in the moved line
                textEditor.SelectionStart = newSelectionStart;
                textEditor.SelectionLength = selectionLength;
                textEditor.CaretOffset = newCaretOffset;

                if (removeLastDelimiter)
                {
                    var ln = textEditor.Document.GetLineByNumber(textEditor.Document.LineCount - 1);
                    textEditor.Document.Remove(ln.EndOffset, ln.DelimiterLength);
                }

                textEditor.Document.EndUpdate();
            }
        }

        private void MoveSelectedLinesDown()
        {
            if (CanMoveLineDown)
            {
                // move selected lines down by moving next line up
                textEditor.Document.BeginUpdate();

                var selectionStart = textEditor.SelectionStart;
                var selectionLength = textEditor.SelectionLength;

                // check for caret in the first column of the line following the selection
                // if so, don't include that line
                int adj = textEditor.TextArea.Caret.VisualColumn == 0 && selectionLength > 0 ? -1 : 0;

                var beginLine = textEditor.Document.GetLineByOffset(selectionStart);
                var finalLine = textEditor.Document.GetLineByOffset(selectionStart + selectionLength + adj);
                var nextLine = finalLine.NextLine;

                var newCaretOffset = textEditor.CaretOffset + nextLine.Length + nextLine.DelimiterLength;
                var newSelectionStart = selectionStart + nextLine.Length + nextLine.DelimiterLength;

                var lineToMove = textEditor.Document.GetText(nextLine.Offset, nextLine.Length + nextLine.DelimiterLength);

                // is nextLine the last line in the document? it won't have a newline
                bool removeLastDelimiter = false;
                if (nextLine.DelimiterLength == 0)
                {
                    lineToMove += Environment.NewLine;
                    newCaretOffset += Environment.NewLine.Length;
                    newSelectionStart += Environment.NewLine.Length;
                    removeLastDelimiter = true;
                }

                textEditor.Document.Remove(nextLine.Offset, nextLine.Length + nextLine.DelimiterLength);
                textEditor.Document.Insert(beginLine.Offset, lineToMove);

                // move the caret to the same position in the moved line
                textEditor.CaretOffset = newCaretOffset;
                textEditor.SelectionStart = newSelectionStart;
                textEditor.SelectionLength = selectionLength;

                if (removeLastDelimiter)
                {
                    var ln = textEditor.Document.GetLineByNumber(textEditor.Document.LineCount - 1);
                    textEditor.Document.Remove(ln.EndOffset, ln.DelimiterLength);
                }

                textEditor.Document.EndUpdate();
            }
        }

        private void Document_TextChanged(object? sender, EventArgs e)
        {
            IsModified = string.CompareOrdinal(originalScript, textEditor.Text) != 0;
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            string title = (IsModified ? "*" : string.Empty) +
                (string.IsNullOrEmpty(ScriptFile) ? Resources.Scripts_NewScript :
                Path.GetFileName(ScriptFile)) + " - " + Resources.Script_Editor_Title;

            WindowTitle = title;
        }

        public void OpenScriptFile(string filePath)
        {
            textEditor.Document.TextChanged -= Document_TextChanged;

            ScriptFile = filePath;
            textEditor.Load(filePath);
            IsModified = false;
            originalScript = textEditor.Text;
            UpdateWindowTitle();

            textEditor.Document.TextChanged += Document_TextChanged;
        }

        private void NewScript()
        {
            if (ConfirmSave())
            {
                textEditor.Document.TextChanged -= Document_TextChanged;

                ScriptFile = string.Empty;
                textEditor.Document = new ICSharpCode.AvalonEdit.Document.TextDocument();
                IsModified = false;
                originalScript = string.Empty;
                UpdateWindowTitle();

                textEditor.Document.TextChanged += Document_TextChanged;
            }
        }

        private void RunScript()
        {
            ValidateScript(false);
            RequestRun?.Invoke(this, EventArgs.Empty);
        }

        private void Save()
        {
            if (IsModified)
            {
                if (!ValidateScript(true))
                {
                    return;
                }

                if (string.IsNullOrEmpty(ScriptFile))
                {
                    if (!SaveAs(false))
                        return;
                }

                textEditor.Document.TextChanged -= Document_TextChanged;

                textEditor.Save(ScriptFile);
                IsModified = false;
                originalScript = textEditor.Text;
                UpdateWindowTitle();

                textEditor.Document.TextChanged += Document_TextChanged;
            }
        }

        private bool firstFileSave = true;
        private bool SaveAs(bool validate)
        {
            if (validate && !ValidateScript(true))
            {
                return false;
            }

            SaveFileDialog dlg = new()
            {
                Filter = Resources.Scripts_ScriptFiles + "|*" + ScriptManager.ScriptExt,
                DefaultExt = ScriptManager.ScriptExt.TrimStart('.'),
            };

            if (firstFileSave)
            {
                firstFileSave = false;
                string dataFolder = Path.Combine(DirectoryConfiguration.Instance.DataDirectory, ScriptManager.ScriptFolder);
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }
                dlg.InitialDirectory = dataFolder;
            }

            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                ScriptFile = dlg.FileName;
                Save();

                NewScriptFileSaved?.Invoke(this, EventArgs.Empty);

                return true;
            }
            else
            {
                return false;
            }
        }

        private void Close()
        {
            if (ConfirmSave())
            {
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        internal bool ConfirmSave()
        {
            if (IsModified)
            {
                string name = string.IsNullOrEmpty(ScriptFile) ? Resources.Scripts_NewScript :
                    "'" + Path.GetFileName(ScriptFile) + "'";

                var result = MessageBox.Show(string.Format(Resources.MessageBox_Scripts_HasUnsavedChangesSaveNow, name),
                    Resources.MessageBox_DnGrep, MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);

                if (result == MessageBoxResult.Yes)
                {
                    Save();
                    return true;
                }
                else if (result == MessageBoxResult.No)
                {
                    return true;
                }

                return false;
            }
            return true;
        }

        private static readonly char[] newlines = ['\n', '\r'];

        private bool ValidateScript(bool doingSave)
        {
            ValidationData.Clear();
            HasValidationErrors = false;

            string[] script = textEditor.Text.Split(newlines, StringSplitOptions.RemoveEmptyEntries);
            var queue = ScriptManager.Instance.ParseScript(script, false);
            var results = ScriptManager.Instance.Validate(queue);

            foreach (var error in results)
            {
                ValidationData.Add(new ValidationErrorViewModel(
                    error.Item1.ToString(),
                    ScriptManager.ToErrorString(error.Item2)));
            }
            HasValidationErrors = ValidationData.Count > 0;

            if (doingSave && results.Count > 0)
            {
                string name = string.IsNullOrEmpty(ScriptFile) ? Resources.Scripts_NewScript :
                    "'" + Path.GetFileName(ScriptFile) + "'";

                var result = MessageBox.Show(string.Format(Resources.MessageBox_Scripts_HasValidationErrorsSaveAnyway, name),
                    Resources.MessageBox_DnGrep, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);

                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public partial class ValidationErrorViewModel : CultureAwareViewModel
    {
        public ValidationErrorViewModel(string line, string message)
        {
            Line = line;
            Message = message;
        }

        [ObservableProperty]
        private string line = string.Empty;

        [ObservableProperty]
        private string message = string.Empty;
    }
}
