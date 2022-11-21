using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using dnGREP.Common;
using ICSharpCode.AvalonEdit;

namespace dnGREP.WPF
{
    public class ScriptViewModel : CultureAwareViewModel
    {
        private readonly TextEditor textEditor;

        public event EventHandler RequestClose;

        public ScriptViewModel(TextEditor textEditor)
        {
            this.textEditor = textEditor;

            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            MainFormFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
            ResultsFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ResultsFontFamily);
            ResultsFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.ResultsFontSize);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            PropertyChanged += ScriptViewModel_PropertyChanged;
        }

        private void ScriptViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScriptFile))
            {
                textEditor.Load(ScriptFile);
            }
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


        private string scriptText = string.Empty;
        public string ScriptText
        {
            get { return scriptText; }
            set
            {
                if (scriptText == value)
                {
                    return;
                }

                scriptText = value;
                OnPropertyChanged(nameof(ScriptText));
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


        private void NewScript()
        {
        }

        private void Save()
        {
        }

        private void SaveAs()
        {
        }

        private void Close()
        {
            if (ConfirmSave())
            {
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool ConfirmSave()
        {
            if (textEditor.IsModified)
            {
                var result = MessageBox.Show("Untitled has unsaved changes. Save changes?",
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

        //private void Cut()
        //{
        //}

        //private void Copy()
        //{
        //}

        //private void Paste()
        //{
        //}

        //private void Delete()
        //{
        //}

    }
}
