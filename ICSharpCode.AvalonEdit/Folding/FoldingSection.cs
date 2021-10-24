// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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

using System.Diagnostics;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Folding
{
	/// <summary>
	/// A section that can be folded.
	/// </summary>
	public sealed class FoldingSection : TextSegment
	{
		readonly FoldingManager manager;
		bool isFolded;
		internal CollapsedLineSection[] collapsedSections;
		string title;

		/// <summary>
		/// Gets/sets if the section is folded.
		/// </summary>
		public bool IsFolded {
			get { return isFolded; }
			set {
				if (isFolded != value) {
					isFolded = value;
					ValidateCollapsedLineSections(); // create/destroy CollapsedLineSection
					manager.Redraw(this);
				}
			}
		}

		internal void ValidateCollapsedLineSections()
		{
			if (!isFolded) {
				RemoveCollapsedLineSection();
				return;
			}
			// It is possible that StartOffset/EndOffset get set to invalid values via the property setters in TextSegment,
			// so we coerce those values into the valid range.
			DocumentLine startLine = manager.document.GetLineByOffset(StartOffset.CoerceValue(0, manager.document.TextLength));
			DocumentLine endLine = manager.document.GetLineByOffset(EndOffset.CoerceValue(0, manager.document.TextLength));
			if (startLine == endLine) {
				RemoveCollapsedLineSection();
			} else {
				if (collapsedSections == null)
					collapsedSections = new CollapsedLineSection[manager.textViews.Count];
				// Validate collapsed line sections
				DocumentLine startLinePlusOne = startLine.NextLine;
				for (int i = 0; i < collapsedSections.Length; i++) {
					var collapsedSection = collapsedSections[i];
					if (collapsedSection == null || collapsedSection.Start != startLinePlusOne || collapsedSection.End != endLine) {
						// recreate this collapsed section
						if (collapsedSection != null) {
							Debug.WriteLine("CollapsedLineSection validation - recreate collapsed section from " + startLinePlusOne + " to " + endLine);
							collapsedSection.Uncollapse();
						}
						collapsedSections[i] = manager.textViews[i].CollapseLines(startLinePlusOne, endLine);
					}
				}
			}
		}

		/// <inheritdoc/>
		protected override void OnSegmentChanged()
		{
			ValidateCollapsedLineSections();
			base.OnSegmentChanged();
			// don't redraw if the FoldingSection wasn't added to the FoldingManager's collection yet
			if (IsConnectedToCollection)
				manager.Redraw(this);
		}

		/// <summary>
		/// Gets/Sets the text used to display the collapsed version of the folding section.
		/// </summary>
		public string Title {
			get {
				return title;
			}
			set {
				if (title != value) {
					title = value;
					if (this.IsFolded)
						manager.Redraw(this);
				}
			}
		}

		/// <summary>
		/// Gets the content of the collapsed lines as text.
		/// </summary>
		public string TextContent {
			get {
				return manager.document.GetText(StartOffset, EndOffset - StartOffset);
			}
		}

		/// <summary>
		/// Gets/Sets an additional object associated with this folding section.
		/// </summary>
		public object Tag { get; set; }

		internal FoldingSection(FoldingManager manager, int startOffset, int endOffset)
		{
			Debug.Assert(manager != null);
			this.manager = manager;
			this.StartOffset = startOffset;
			this.Length = endOffset - startOffset;
		}

		void RemoveCollapsedLineSection()
		{
			if (collapsedSections != null) {
				foreach (var collapsedSection in collapsedSections) {
					if (collapsedSection != null && collapsedSection.Start != null)
						collapsedSection.Uncollapse();
				}
				collapsedSections = null;
			}
		}
	}
}
