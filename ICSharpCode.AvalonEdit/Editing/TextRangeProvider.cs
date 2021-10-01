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
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation.Provider;
using System.Windows.Automation.Text;
using System.Windows.Documents;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Editing
{
	class TextRangeProvider : ITextRangeProvider
	{
		readonly TextArea textArea;
		readonly TextDocument doc;
		ISegment segment;

		public TextRangeProvider(TextArea textArea, TextDocument doc, ISegment segment)
		{
			this.textArea = textArea;
			this.doc = doc;
			this.segment = segment;
		}

		public TextRangeProvider(TextArea textArea, TextDocument doc, int offset, int length)
		{
			this.textArea = textArea;
			this.doc = doc;
			this.segment = new AnchorSegment(doc, offset, length);
		}

		string ID {
			get {
				return string.Format("({0}: {1})", GetHashCode().ToString("x8"), segment);
			}
		}

		[Conditional("DEBUG")]
		static void Log(string format, params object[] args)
		{
			Debug.WriteLine(string.Format(format, args));
		}

		public void AddToSelection()
		{
			Log("{0}.AddToSelection()", ID);
		}

		public ITextRangeProvider Clone()
		{
			var result = new TextRangeProvider(textArea, doc, segment);
			Log("{0}.Clone() = {1}", ID, result.ID);
			return result;
		}

		public bool Compare(ITextRangeProvider range)
		{
			TextRangeProvider other = (TextRangeProvider)range;
			bool result = doc == other.doc
				&& segment.Offset == other.segment.Offset
				&& segment.EndOffset == other.segment.EndOffset;
			Log("{0}.Compare({1}) = {2}", ID, other.ID, result);
			return result;
		}

		int GetEndpoint(TextPatternRangeEndpoint endpoint)
		{
			switch (endpoint) {
				case TextPatternRangeEndpoint.Start:
					return segment.Offset;
				case TextPatternRangeEndpoint.End:
					return segment.EndOffset;
				default:
					throw new ArgumentOutOfRangeException("endpoint");
			}
		}

		public int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
		{
			TextRangeProvider other = (TextRangeProvider)targetRange;
			int result = GetEndpoint(endpoint).CompareTo(other.GetEndpoint(targetEndpoint));
			Log("{0}.CompareEndpoints({1}, {2}, {3}) = {4}", ID, endpoint, other.ID, targetEndpoint, result);
			return result;
		}

		public void ExpandToEnclosingUnit(TextUnit unit)
		{
			Log("{0}.ExpandToEnclosingUnit({1})", ID, unit);
			switch (unit) {
				case TextUnit.Character:
					ExpandToEnclosingUnit(CaretPositioningMode.Normal);
					break;
				case TextUnit.Format:
				case TextUnit.Word:
					ExpandToEnclosingUnit(CaretPositioningMode.WordStartOrSymbol);
					break;
				case TextUnit.Line:
				case TextUnit.Paragraph:
					segment = doc.GetLineByOffset(segment.Offset);
					break;
				case TextUnit.Document:
					segment = new AnchorSegment(doc, 0, doc.TextLength);
					break;
			}
		}

		private void ExpandToEnclosingUnit(CaretPositioningMode mode)
		{
			int start = TextUtilities.GetNextCaretPosition(doc, segment.Offset + 1, LogicalDirection.Backward, mode);
			if (start < 0)
				return;
			int end = TextUtilities.GetNextCaretPosition(doc, start, LogicalDirection.Forward, mode);
			if (end < 0)
				return;
			segment = new AnchorSegment(doc, start, end - start);
		}

		public ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
		{
			Log("{0}.FindAttribute({1}, {2}, {3})", ID, attribute, value, backward);
			return null;
		}

		public ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
		{
			Log("{0}.FindText({1}, {2}, {3})", ID, text, backward, ignoreCase);
			string segmentText = doc.GetText(segment);
			var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			int pos = backward ? segmentText.LastIndexOf(text, comparison) : segmentText.IndexOf(text, comparison);
			if (pos >= 0) {
				return new TextRangeProvider(textArea, doc, segment.Offset + pos, text.Length);
			}
			return null;
		}

		public object GetAttributeValue(int attribute)
		{
			Log("{0}.GetAttributeValue({1})", ID, attribute);
			return null;
		}

		public double[] GetBoundingRectangles()
		{
			Log("{0}.GetBoundingRectangles()", ID);
			var textView = textArea.TextView;
			var source = PresentationSource.FromVisual(this.textArea);
			var result = new List<double>();
			foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment)) {
				var tl = textView.PointToScreen(rect.TopLeft);
				var br = textView.PointToScreen(rect.BottomRight);
				result.Add(tl.X);
				result.Add(tl.Y);
				result.Add(br.X - tl.X);
				result.Add(br.Y - tl.Y);
			}
			return result.ToArray();
		}

		public IRawElementProviderSimple[] GetChildren()
		{
			Log("{0}.GetChildren()", ID);
			return new IRawElementProviderSimple[0];
		}

		public IRawElementProviderSimple GetEnclosingElement()
		{
			Log("{0}.GetEnclosingElement()", ID);
			var peer = TextAreaAutomationPeer.FromElement(textArea) as TextAreaAutomationPeer;
			if (peer == null)
				throw new NotSupportedException();
			return peer.Provider;
		}

		public string GetText(int maxLength)
		{
			Log("{0}.GetText({1})", ID, maxLength);
			if (maxLength < 0)
				return doc.GetText(segment);
			else
				return doc.GetText(segment.Offset, Math.Min(segment.Length, maxLength));
		}

		public int Move(TextUnit unit, int count)
		{
			Log("{0}.Move({1}, {2})", ID, unit, count);
			int movedCount = MoveEndpointByUnit(TextPatternRangeEndpoint.Start, unit, count);
			segment = new SimpleSegment(segment.Offset, 0); // Collapse to empty range
			ExpandToEnclosingUnit(unit);
			return movedCount;
		}

		public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
		{
			TextRangeProvider other = (TextRangeProvider)targetRange;
			Log("{0}.MoveEndpointByRange({1}, {2}, {3})", ID, endpoint, other.ID, targetEndpoint);
			SetEndpoint(endpoint, other.GetEndpoint(targetEndpoint));
		}

		void SetEndpoint(TextPatternRangeEndpoint endpoint, int targetOffset)
		{
			if (endpoint == TextPatternRangeEndpoint.Start) {
				// set start of this range to targetOffset
				segment = new AnchorSegment(doc, targetOffset, Math.Max(0, segment.EndOffset - targetOffset));
			} else {
				// set end of this range to targetOffset
				int newStart = Math.Min(segment.Offset, targetOffset);
				segment = new AnchorSegment(doc, newStart, targetOffset - newStart);
			}
		}

		public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
		{
			Log("{0}.MoveEndpointByUnit({1}, {2}, {3})", ID, endpoint, unit, count);
			int offset = GetEndpoint(endpoint);
			switch (unit) {
				case TextUnit.Character:
					offset = MoveOffset(offset, CaretPositioningMode.Normal, count);
					break;
				case TextUnit.Format:
				case TextUnit.Word:
					offset = MoveOffset(offset, CaretPositioningMode.WordStart, count);
					break;
				case TextUnit.Line:
				case TextUnit.Paragraph:
					int line = doc.GetLineByOffset(offset).LineNumber;
					int newLine = Math.Max(1, Math.Min(doc.LineCount, line + count));
					offset = doc.GetLineByNumber(newLine).Offset;
					break;
				case TextUnit.Document:
					offset = count < 0 ? 0 : doc.TextLength;
					break;
			}
			SetEndpoint(endpoint, offset);
			return count;
		}

		private int MoveOffset(int offset, CaretPositioningMode mode, int count)
		{
			var direction = count < 0 ? LogicalDirection.Backward : LogicalDirection.Forward;
			count = Math.Abs(count);
			for (int i = 0; i < count; i++) {
				int newOffset = TextUtilities.GetNextCaretPosition(doc, offset, direction, mode);
				if (newOffset == offset || newOffset < 0)
					break;
				offset = newOffset;
			}
			return offset;
		}

		public void RemoveFromSelection()
		{
			Log("{0}.RemoveFromSelection()", ID);
		}

		public void ScrollIntoView(bool alignToTop)
		{
			Log("{0}.ScrollIntoView({1})", ID, alignToTop);
		}

		public void Select()
		{
			Log("{0}.Select()", ID);
			textArea.Selection = new SimpleSelection(textArea,
				new TextViewPosition(doc.GetLocation(segment.Offset)),
				new TextViewPosition(doc.GetLocation(segment.EndOffset)));
		}
	}
}
