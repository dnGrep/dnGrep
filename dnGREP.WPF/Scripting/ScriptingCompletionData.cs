using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnGREP.WPF
{
    public class ScriptingCompletionData : ICompletionData
    {
        public ScriptingCompletionData(string text)
        {
            Text = text;
        }

        public ImageSource Image { get; set; }

        public string Text { get; set; } = string.Empty;

        // Use this property if you want to show a fancy UIElement in the drop down list.
        public object Content
        {
            get { return Text; }
        }

        // displays as a tool tip
        public object Description { get; set; } = null;
        public double Priority { get; set; } = 0;

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        {
            var caret = textArea.Caret;
            var docLine = textArea.Document.GetLineByNumber(caret.Line);
            var lineText = textArea.Document.GetText(docLine.Offset, docLine.Length);
            VisualLine visualLine = textArea.TextView.GetVisualLine(caret.Line);
            int offsetStart = visualLine.GetNextCaretPosition(caret.VisualColumn, LogicalDirection.Backward, CaretPositioningMode.WordBorder, true);

            string completionText = Text;
            if (offsetStart > -1)
            {
                string existing = lineText.Substring(offsetStart, caret.VisualColumn - offsetStart);
                if (completionSegment.Length == 0 && !string.IsNullOrEmpty(existing) && Text.StartsWith(existing))
                {
                    completionText = Text.Remove(0, existing.Length);
                }
            }

            textArea.Document.Replace(completionSegment, completionText);
        }

        internal static IList<ScriptingCompletionData> commands = new List<ScriptingCompletionData>();
        internal static IList<ScriptingCompletionData> setTargets = new List<ScriptingCompletionData>();
        internal static IList<ScriptingCompletionData> useTargets = new List<ScriptingCompletionData>();
        internal static IList<ScriptingCompletionData> addTargets = new List<ScriptingCompletionData>();
        internal static IList<ScriptingCompletionData> removeTargets = new List<ScriptingCompletionData>();
        internal static IList<ScriptingCompletionData> reportTargets = new List<ScriptingCompletionData>();

        static ScriptingCompletionData()
        {
            foreach (string cmd in ScriptManager.CommandNames.OrderBy(s => s))
            {
                commands.Add(new ScriptingCompletionData(cmd));
            }

            foreach (string target in MainViewModel.SetCommandMap.Keys.OrderBy(s => s))
            {
                setTargets.Add(new ScriptingCompletionData(target));
            }

            foreach (string target in MainViewModel.UseCommandMap.Keys.OrderBy(s => s))
            {
                useTargets.Add(new ScriptingCompletionData(target));
            }

            foreach (string target in MainViewModel.AddCommandMap.Keys.OrderBy(s => s))
            {
                addTargets.Add(new ScriptingCompletionData(target));
            }

            foreach (string target in MainViewModel.RemoveCommandMap.Keys.OrderBy(s => s))
            {
                removeTargets.Add(new ScriptingCompletionData(target));
            }

            foreach (string target in MainViewModel.ReportCommandMap.Keys.OrderBy(s => s))
            {
                reportTargets.Add(new ScriptingCompletionData(target));
            }
        }
    }
}
