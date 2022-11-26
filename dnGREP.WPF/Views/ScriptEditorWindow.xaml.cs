using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Search;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for ScriptEditorWindow.xaml
    /// </summary>
    public partial class ScriptEditorWindow : ThemedWindow
    {
        private readonly ScriptViewModel viewModel;
        public event EventHandler NewScriptFileSaved;
        public event EventHandler RequestRun;

        public ScriptEditorWindow()
        {
            InitializeComponent();

            viewModel = new ScriptViewModel(textEditor);
            DataContext = viewModel;

            viewModel.RequestRun += (s, e) => RequestRun?.Invoke(this, e);
            viewModel.RequestClose += (s, e) => Close();
            viewModel.RequestSuggest += (s, e) => Suggest();
            viewModel.NewScriptFileSaved += (s, e) => NewScriptFileSaved?.Invoke(this, e);

            SearchPanel.Install(textEditor);

            var definition = ThemedHighlightingManager.Instance.GetDefinitionByExtension(ScriptManager.ScriptExt);
            textEditor.SyntaxHighlighting = definition;
            textEditor.TextArea.TextEntering += TextArea_TextEntering;
            textEditor.TextArea.TextEntered += TextArea_TextEntered;
            textEditor.TextArea.KeyDown += TextArea_KeyDown;
            textEditor.TextArea.KeyUp += TextArea_KeyUp;

            Closing += ScriptEditorWindow_Closing;

            DiginesisHelpProvider.HelpNamespace = "https://github.com/dnGrep/dnGrep/wiki/";
            DiginesisHelpProvider.ShowHelp = true;
        }

        public bool ConfirmSave()
        {
            bool okClose = viewModel.ConfirmSave();
            if (!okClose)
            {
                Topmost = true;
                Focus();
                Topmost = false;
            }

            return okClose;
        }

        private void ScriptEditorWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!viewModel.ConfirmSave())
            {
                e.Cancel = true;
            }
        }

        public string ScriptFile => viewModel.ScriptFile;

        public IEnumerable<string> ScriptText
        {
            get
            {
                if (textEditor != null)
                {
                    string text = textEditor.Text;
                    if (!string.IsNullOrEmpty(text))
                    {
                        return text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
                return Enumerable.Empty<string>();
            }
        }

        public void OpenScriptFile(string filePath)
        {
            viewModel.OpenScriptFile(filePath);
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
            var caret = textEditor.TextArea.Caret;

            if (e.Text == " " || caret.VisualColumn < 2)
            {
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
            var stmt = ScriptManager.Instance.ParseLine(lineText, caret.Line);

            if (stmt != null && wordIndex == 0)
            {
                wordIndex++;
            }

            List<ScriptingCompletionData> scriptingData = null;
            string valueHint = null;

            if (wordIndex == 0)
            {
                scriptingData = ScriptManager.CommandCompletionData;
            }
            else if (stmt != null && wordIndex == 1)
            {
                var cmd = ScriptManager.ScriptCommands.FirstOrDefault(c => c.Command == stmt.Command);
                if (cmd != null)
                {
                    if (cmd.CompletionData.Count > 0)
                    {
                        scriptingData = cmd.CompletionData;
                    }
                    else if (!string.IsNullOrEmpty(cmd.ValueHint))
                    {
                        valueHint = cmd.ValueHint;
                    }
                }
            }
            else if (stmt != null && !string.IsNullOrEmpty(stmt.Target))
            {
                var cmd = ScriptManager.ScriptCommands.FirstOrDefault(c => c.Command == stmt.Command);
                if (cmd != null)
                {
                    var target = cmd.Targets.FirstOrDefault(t => t.Target == stmt.Target);
                    if (target != null)
                    {
                        if (target.CompletionData.Count > 0)
                        {
                            scriptingData = target.CompletionData;
                        }
                        else if (!string.IsNullOrEmpty(target.ValueHint))
                        {
                            valueHint = target.ValueHint;
                        }
                    }
                }
            }

            if (scriptingData != null && scriptingData.Count > 0)
            {
                completionWindow = new CompletionWindow(textEditor.TextArea)
                {
                    FontSize = viewModel.ResultsFontSize,
                    Width = double.NaN,
                    SizeToContent = System.Windows.SizeToContent.WidthAndHeight
                };
                AddRange(scriptingData, completionWindow.CompletionList.CompletionData);
                completionWindow.Closed += (s, e) =>
                {
                    completionWindow = null;
                };
                completionWindow.Show();
            }
            else if (!string.IsNullOrEmpty(valueHint))
            {
                var insightWindow = new InsightWindow(textEditor.TextArea)
                {
                    Content = valueHint,
                    //Background = System.Windows.Media.Brushes.Linen
                };
                insightWindow.Closed += (s, e) =>
                {
                    completionWindow = null;
                };
                insightWindow.Show();
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
    }
}
