using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for ScriptEditorWindow.xaml
    /// </summary>
    public partial class ScriptEditorWindow : ThemedWindow
    {
        private readonly ScriptViewModel viewModel;

        public ScriptEditorWindow()
        {
            InitializeComponent();

            viewModel = new ScriptViewModel(textEditor);
            DataContext = viewModel;

            viewModel.RequestClose += (s, e) => Close();

            SearchPanel.Install(textEditor);

            textEditor.ShowLineNumbers = true;

            textEditor.TextArea.TextEntering += TextArea_TextEntering;
            textEditor.TextArea.TextEntered += TextArea_TextEntered;
            textEditor.TextArea.KeyDown += TextArea_KeyDown;
            textEditor.TextArea.KeyUp += TextArea_KeyUp;
        }

        CompletionWindow completionWindow;
        void TextArea_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                Suggest();
                e.Handled = true;
            }

        }

        private void TextArea_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                Suggest();
                e.Handled = true;
            }
        }

        private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == " " || e.Text == "\n")
            {
                var caret = textEditor.TextArea.Caret;
                string lineText = GetLineText(caret);

                if (lineText.StartsWith("//"))
                {
                    return;
                }

                if (e.Text == "\n" && !string.IsNullOrEmpty(lineText))
                {
                    return;
                }

                Suggest();
            }
        }

        private void Suggest()
        {
            var caret = textEditor.TextArea.Caret;
            string lineText = GetLineText(caret);
            int wordIndex = GetWordIndex(lineText, caret);
            //string word = GetWordAtCaret(lineText, caret);
            var stmt = ScriptManager.Instance.ParseLine(lineText, caret.Line);

            if (stmt != null && wordIndex == 0)
            {
                wordIndex++;
            }

            completionWindow = new CompletionWindow(textEditor.TextArea);
            // provide AvalonEdit with the data:
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            if (wordIndex == 0)
            {
                AddRange(ScriptingCompletionData.commands, data);
            }
            else if (wordIndex == 1)
            {
                if (stmt.Command == "set")
                {
                    AddRange(ScriptingCompletionData.setTargets, data);
                }
                else if (stmt.Command == "use")
                {
                    AddRange(ScriptingCompletionData.useTargets, data);
                }
                else if (stmt.Command == "add")
                {
                    AddRange(ScriptingCompletionData.addTargets, data);
                }
                else if (stmt.Command == "remove")
                {
                    AddRange(ScriptingCompletionData.removeTargets, data);
                }
                else if (stmt.Command == "report")
                {
                    AddRange(ScriptingCompletionData.reportTargets, data);
                }
            }
            if (data.Count > 0)
            {
                completionWindow.Show();
                completionWindow.Closed += delegate
                {
                    completionWindow = null;
                };
            }
        }

        private void AddRange(IList<ScriptingCompletionData> source, IList<ICompletionData> destination)
        {
            foreach (var item in source)
            {
                destination.Add(item);
            }
        }

        private string GetLineText(Caret caret)
        {
            var docLine = textEditor.Document.GetLineByNumber(caret.Line);
            return textEditor.Document.GetText(docLine.Offset, docLine.Length);
        }

        private int GetWordIndex(string lineText, Caret caret)
        {
            return lineText.Substring(0, caret.VisualColumn).TrimStart().Count(c => c == ' ');
        }

        private string GetWordAtCaret(string lineText, Caret caret)
        {
            VisualLine visualLine = textEditor.TextArea.TextView.GetVisualLine(caret.Line);
            int offsetStart = visualLine.GetNextCaretPosition(caret.VisualColumn, LogicalDirection.Backward, CaretPositioningMode.WordBorder, true);
            int offsetEnd = visualLine.GetNextCaretPosition(caret.VisualColumn, LogicalDirection.Forward, CaretPositioningMode.WordBorder, true);

            if (offsetEnd == -1 || offsetStart == -1)
                return string.Empty;

            var currentChar = lineText.Substring(caret.VisualColumn, 1);

            if (string.IsNullOrWhiteSpace(currentChar))
                return string.Empty;

            return lineText.Substring(offsetStart, offsetEnd - offsetStart);
        }

        private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // do not set e.Handled=true - we still want to insert the character that was typed
        }

        public string ScriptFile
        {
            get => viewModel.ScriptFile;
            set => viewModel.ScriptFile = value;
        }
    }
}
