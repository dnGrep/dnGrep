// Copyright (c) 2016 Daniel Grunwald
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Editing
{
	class TextAreaAutomationPeer : FrameworkElementAutomationPeer, IValueProvider, ITextProvider
	{
		public TextAreaAutomationPeer(TextArea owner)
			: base(owner)
		{
			owner.Caret.PositionChanged += OnSelectionChanged;
			owner.SelectionChanged += OnSelectionChanged;
		}

		private void OnSelectionChanged(object sender, EventArgs e)
		{
			Debug.WriteLine("RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged)");
			RaiseAutomationEvent(AutomationEvents.TextPatternOnTextSelectionChanged);
		}

		private TextArea TextArea { get { return (TextArea)base.Owner; } }

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Document;
		}

		internal IRawElementProviderSimple Provider {
			get { return ProviderFromPeer(this); }
		}

		public bool IsReadOnly {
			get { return TextArea.ReadOnlySectionProvider == ReadOnlySectionDocument.Instance; }
		}

		public void SetValue(string value)
		{
			TextArea.Document.Text = value;
		}

		public string Value {
			get { return TextArea.Document.Text; }
		}

		public ITextRangeProvider DocumentRange {
			get {
				Debug.WriteLine("TextAreaAutomationPeer.get_DocumentRange()");
				return new TextRangeProvider(TextArea, TextArea.Document, 0, TextArea.Document.TextLength);
			}
		}

		public ITextRangeProvider[] GetSelection()
		{
			Debug.WriteLine("TextAreaAutomationPeer.GetSelection()");
			if (TextArea.Selection.IsEmpty) {
				var anchor = TextArea.Document.CreateAnchor(TextArea.Caret.Offset);
				anchor.SurviveDeletion = true;
				return new ITextRangeProvider[] { new TextRangeProvider(TextArea, TextArea.Document, new AnchorSegment(anchor, anchor)) };
			}
			return TextArea.Selection.Segments.Select(s => new TextRangeProvider(TextArea, TextArea.Document, s)).ToArray();
		}

		public ITextRangeProvider[] GetVisibleRanges()
		{
			Debug.WriteLine("TextAreaAutomationPeer.GetVisibleRanges()");
			throw new NotImplementedException();
		}

		public ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement)
		{
			Debug.WriteLine("TextAreaAutomationPeer.RangeFromChild()");
			throw new NotImplementedException();
		}

		public ITextRangeProvider RangeFromPoint(System.Windows.Point screenLocation)
		{
			Debug.WriteLine("TextAreaAutomationPeer.RangeFromPoint()");
			throw new NotImplementedException();
		}

		public SupportedTextSelection SupportedTextSelection {
			get { return SupportedTextSelection.Single; }
		}

		public override object GetPattern(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Text)
				return this;
			if (patternInterface == PatternInterface.Value)
				return this;
			if (patternInterface == PatternInterface.Scroll) {
				TextEditor editor = TextArea.GetService(typeof(TextEditor)) as TextEditor;
				if (editor != null) {
					var fromElement = FromElement(editor);
					if (fromElement != null)
						return fromElement.GetPattern(patternInterface);
				}
			}
			return base.GetPattern(patternInterface);
		}
	}
}
