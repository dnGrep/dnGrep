using System;
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
        public ScriptingCompletionData(ScriptCommandDefinition def)
        {
            Text = def.Command;
            Priority = def.Priority;
            Description = def.Description;
        }

        public ScriptingCompletionData(ScriptTargetDefinition def)
        {
            Text = def.Target;
            Priority = def.Priority;
            Description = def.Description;
        }

        public ScriptingCompletionData(ScriptValueDefinition def)
        {
            Text = def.Value;
            Priority = def.Priority;
            Description = def.Description;
        }

        public ImageSource Image => null;

        public string Text { get; private set; } = string.Empty;

        public object Content
        {
            get { return Text; }
        }

        // displays as a tool tip
        public object Description { get; private set; }

        public double Priority { get; private set; } = 0;

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
    }
}
