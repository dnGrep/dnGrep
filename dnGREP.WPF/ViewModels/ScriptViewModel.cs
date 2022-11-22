using System;
using System.Windows;
using System.Windows.Input;
using Alphaleonis.Win32.Filesystem;
using dnGREP.Common;
using ICSharpCode.AvalonEdit;
using Microsoft.Win32;

namespace dnGREP.WPF
{
    public class ScriptViewModel : CultureAwareViewModel
    {
        private readonly TextEditor textEditor;
        private string originalScript = string.Empty;

        public event EventHandler RequestClose;
        public event EventHandler NewScriptFileSaved;

        public ScriptViewModel(TextEditor textEditor)
        {
            this.textEditor = textEditor;

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            ResultsFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsFontSize);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            textEditor.Document.TextChanged += Document_TextChanged;
        }


        private string applicationFontFamily;
        public string ApplicationFontFamily
        {
            get { return applicationFontFamily; }
            set
            {
                if (applicationFontFamily == value)
                    return;

                applicationFontFamily = value;
                base.OnPropertyChanged(nameof(ApplicationFontFamily));
            }
        }

        private double mainFormfontSize;
        public double MainFormFontSize
        {
            get { return mainFormfontSize; }
            set
            {
                if (mainFormfontSize == value)
                    return;

                mainFormfontSize = value;
                base.OnPropertyChanged(nameof(MainFormFontSize));
            }
        }

        private string resultsFontFamily;
        public string ResultsFontFamily
        {
            get { return resultsFontFamily; }
            set
            {
                if (resultsFontFamily == value)
                    return;

                resultsFontFamily = value;
                base.OnPropertyChanged(nameof(ResultsFontFamily));
            }
        }

        private double resultsfontSize;
        public double ResultsFontSize
        {
            get { return resultsfontSize; }
            set
            {
                if (resultsfontSize == value)
                    return;

                resultsfontSize = value;
                base.OnPropertyChanged(nameof(ResultsFontSize));
            }
        }

        private double dialogfontSize;
        public double DialogFontSize
        {
            get { return dialogfontSize; }
            set
            {
                if (dialogfontSize == value)
                    return;

                dialogfontSize = value;
                base.OnPropertyChanged(nameof(DialogFontSize));
            }
        }


        private string windowTitle = Localization.Properties.Resources.Script_Editor_Title;
        public string WindowTitle
        {
            get { return windowTitle; }
            set
            {
                if (windowTitle == value)
                {
                    return;
                }

                windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }


        private string scriptFile = string.Empty;
        public string ScriptFile
        {
            get { return scriptFile; }
            set
            {
                if (scriptFile == value)
                {
                    return;
                }

                scriptFile = value;
                OnPropertyChanged(nameof(ScriptFile));
            }
        }


        private bool isModified = false;
        public bool IsModified
        {
            get { return isModified; }
            set
            {
                if (isModified == value)
                {
                    return;
                }

                isModified = value;
                OnPropertyChanged(nameof(IsModified));
            }
        }


        public ICommand NewCommand => new RelayCommand(
            p => NewScript(),
            q => true);

        public ICommand SaveCommand => new RelayCommand(
            p => Save(),
            q => true);

        public ICommand SaveAsCommand => new RelayCommand(
            p => SaveAs(),
            q => true);

        public ICommand CloseCommand => new RelayCommand(
            p => Close(),
            q => true);

        public ICommand UndoCommand => new RelayCommand(
            p => textEditor.Undo(),
            q => textEditor.CanUndo);

        public ICommand RedoCommand => new RelayCommand(
            p => textEditor.Redo(),
            q => textEditor.CanRedo);

        public ICommand CutCommand => new RelayCommand(
            p => textEditor.Cut(),
            q => CanCutOrCopy);

        public ICommand CopyCommand => new RelayCommand(
            p => textEditor.Copy(),
            q => CanCutOrCopy);

        public ICommand PasteCommand => new RelayCommand(
            p => textEditor.Paste(),
            q => CanPaste);

        public ICommand DeleteCommand => new RelayCommand(
            p => textEditor.Delete(),
            q => textEditor.TextArea != null && textEditor.TextArea.Document != null);

        public ICommand DeleteLineCommand => new RelayCommand(
            p => AvalonEditCommands.DeleteLine.Execute(null, textEditor.TextArea),
            q => textEditor.TextArea != null && textEditor.TextArea.Document != null);

        private bool CanCutOrCopy =>
            textEditor.TextArea != null && textEditor.TextArea.Document != null &&
            (textEditor.TextArea.Options.CutCopyWholeLine || !textEditor.TextArea.Selection.IsEmpty);

        public bool CanPaste =>
            textEditor.TextArea != null && textEditor.TextArea.Document != null &&
            textEditor.TextArea.ReadOnlySectionProvider.CanInsert(textEditor.TextArea.Caret.Offset)
                    && Clipboard.ContainsText();


        private void Document_TextChanged(object sender, EventArgs e)
        {
            IsModified = string.CompareOrdinal(originalScript, textEditor.Text) != 0;
            UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            string title = (IsModified ? "*" : string.Empty) +
                (string.IsNullOrEmpty(ScriptFile) ? "New script" : Path.GetFileName(ScriptFile)) +
                " - " + Localization.Properties.Resources.Script_Editor_Title;

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

        private void Save()
        {
            if (IsModified)
            {
                if (string.IsNullOrEmpty(ScriptFile))
                {
                    SaveAs();
                }

                textEditor.Document.TextChanged -= Document_TextChanged;

                textEditor.Save(ScriptFile);
                IsModified = false;
                originalScript = textEditor.Text;
                UpdateWindowTitle();

                textEditor.Document.TextChanged += Document_TextChanged;
            }
        }

        private void SaveAs()
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Script files|*" + ScriptManager.ScriptExt,
                DefaultExt = ScriptManager.ScriptExt.TrimStart('.'),
            };
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                ScriptFile = dlg.FileName;
                Save();

                NewScriptFileSaved?.Invoke(this, EventArgs.Empty);
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
                string name = string.IsNullOrEmpty(ScriptFile) ? "New script" :
                    "'" + Path.GetFileName(ScriptFile) + "'";

                var result = MessageBox.Show(string.Format("{0} has unsaved changes. Save now?", name),
                    "dnGrep", MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
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
    }
}
