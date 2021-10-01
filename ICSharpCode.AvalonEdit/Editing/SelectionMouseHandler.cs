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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Handles selection of text using the mouse.
	/// </summary>
	sealed class SelectionMouseHandler : ITextAreaInputHandler
	{
		readonly TextArea textArea;

		MouseSelectionMode mode;
		AnchorSegment startWord;
		Point possibleDragStartMousePos;

		#region Constructor + Attach + Detach
		internal SelectionMouseHandler(TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			this.textArea = textArea;
		}

		static SelectionMouseHandler()
		{
			EventManager.RegisterClassHandler(typeof(TextArea), Mouse.LostMouseCaptureEvent, new MouseEventHandler(OnLostMouseCapture));
		}

		private static void OnLostMouseCapture(object sender, MouseEventArgs e)
		{
			TextArea textArea = (TextArea)sender;
			if (Mouse.Captured != textArea) {
				SelectionMouseHandler handler = textArea.DefaultInputHandler.MouseSelection as SelectionMouseHandler;
				if (handler != null)
					handler.mode = MouseSelectionMode.None;
			}
		}

		TextArea ITextAreaInputHandler.TextArea {
			get { return textArea; }
		}

		void ITextAreaInputHandler.Attach()
		{
			textArea.MouseLeftButtonDown += textArea_MouseLeftButtonDown;
			textArea.MouseMove += textArea_MouseMove;
			textArea.MouseLeftButtonUp += textArea_MouseLeftButtonUp;
			textArea.QueryCursor += textArea_QueryCursor;
			textArea.DocumentChanged += textArea_DocumentChanged;
			textArea.OptionChanged += textArea_OptionChanged;

			enableTextDragDrop = textArea.Options.EnableTextDragDrop;
			if (enableTextDragDrop) {
				AttachDragDrop();
			}
		}

		void ITextAreaInputHandler.Detach()
		{
			mode = MouseSelectionMode.None;
			textArea.MouseLeftButtonDown -= textArea_MouseLeftButtonDown;
			textArea.MouseMove -= textArea_MouseMove;
			textArea.MouseLeftButtonUp -= textArea_MouseLeftButtonUp;
			textArea.QueryCursor -= textArea_QueryCursor;
			textArea.DocumentChanged -= textArea_DocumentChanged;
			textArea.OptionChanged -= textArea_OptionChanged;
			if (enableTextDragDrop) {
				DetachDragDrop();
			}
		}

		void AttachDragDrop()
		{
			textArea.AllowDrop = true;
			textArea.GiveFeedback += textArea_GiveFeedback;
			textArea.QueryContinueDrag += textArea_QueryContinueDrag;
			textArea.DragEnter += textArea_DragEnter;
			textArea.DragOver += textArea_DragOver;
			textArea.DragLeave += textArea_DragLeave;
			textArea.Drop += textArea_Drop;
		}

		void DetachDragDrop()
		{
			textArea.AllowDrop = false;
			textArea.GiveFeedback -= textArea_GiveFeedback;
			textArea.QueryContinueDrag -= textArea_QueryContinueDrag;
			textArea.DragEnter -= textArea_DragEnter;
			textArea.DragOver -= textArea_DragOver;
			textArea.DragLeave -= textArea_DragLeave;
			textArea.Drop -= textArea_Drop;
		}

		bool enableTextDragDrop;

		void textArea_OptionChanged(object sender, PropertyChangedEventArgs e)
		{
			bool newEnableTextDragDrop = textArea.Options.EnableTextDragDrop;
			if (newEnableTextDragDrop != enableTextDragDrop) {
				enableTextDragDrop = newEnableTextDragDrop;
				if (newEnableTextDragDrop)
					AttachDragDrop();
				else
					DetachDragDrop();
			}
		}

		void textArea_DocumentChanged(object sender, EventArgs e)
		{
			if (mode != MouseSelectionMode.None) {
				mode = MouseSelectionMode.None;
				textArea.ReleaseMouseCapture();
			}
			startWord = null;
		}
		#endregion

		#region Dropping text
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_DragEnter(object sender, DragEventArgs e)
		{
			try {
				e.Effects = GetEffect(e);
				textArea.Caret.Show();
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_DragOver(object sender, DragEventArgs e)
		{
			try {
				e.Effects = GetEffect(e);
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}

		DragDropEffects GetEffect(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.UnicodeText, true)) {
				e.Handled = true;
				int visualColumn;
				bool isAtEndOfLine;
				int offset = GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out visualColumn, out isAtEndOfLine);
				if (offset >= 0) {
					textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
					textArea.Caret.DesiredXPos = double.NaN;
					if (textArea.ReadOnlySectionProvider.CanInsert(offset)) {
						if ((e.AllowedEffects & DragDropEffects.Move) == DragDropEffects.Move
							&& (e.KeyStates & DragDropKeyStates.ControlKey) != DragDropKeyStates.ControlKey) {
							return DragDropEffects.Move;
						} else {
							return e.AllowedEffects & DragDropEffects.Copy;
						}
					}
				}
			}
			return DragDropEffects.None;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_DragLeave(object sender, DragEventArgs e)
		{
			try {
				e.Handled = true;
				if (!textArea.IsKeyboardFocusWithin)
					textArea.Caret.Hide();
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_Drop(object sender, DragEventArgs e)
		{
			try {
				DragDropEffects effect = GetEffect(e);
				e.Effects = effect;
				if (effect != DragDropEffects.None) {
					int start = textArea.Caret.Offset;
					if (mode == MouseSelectionMode.Drag && textArea.Selection.Contains(start)) {
						Debug.WriteLine("Drop: did not drop: drop target is inside selection");
						e.Effects = DragDropEffects.None;
					} else {
						Debug.WriteLine("Drop: insert at " + start);

						var pastingEventArgs = new DataObjectPastingEventArgs(e.Data, true, DataFormats.UnicodeText);
						textArea.RaiseEvent(pastingEventArgs);
						if (pastingEventArgs.CommandCancelled)
							return;

						string text = EditingCommandHandler.GetTextToPaste(pastingEventArgs, textArea);
						if (text == null)
							return;
						bool rectangular = pastingEventArgs.DataObject.GetDataPresent(RectangleSelection.RectangularSelectionDataType);

						// Mark the undo group with the currentDragDescriptor, if the drag
						// is originating from the same control. This allows combining
						// the undo groups when text is moved.
						textArea.Document.UndoStack.StartUndoGroup(this.currentDragDescriptor);
						try {
							if (rectangular && RectangleSelection.PerformRectangularPaste(textArea, textArea.Caret.Position, text, true)) {

							} else {
								textArea.Document.Insert(start, text);
								textArea.Selection = Selection.Create(textArea, start, start + text.Length);
							}
						} finally {
							textArea.Document.UndoStack.EndUndoGroup();
						}
					}
					e.Handled = true;
				}
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}

		void OnDragException(Exception ex)
		{
			// WPF swallows exceptions during drag'n'drop or reports them incorrectly, so
			// we re-throw them later to allow the application's unhandled exception handler
			// to catch them
			textArea.Dispatcher.BeginInvoke(
				DispatcherPriority.Send,
				new Action(delegate {
					throw new DragDropException("Exception during drag'n'drop", ex);
				}));
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_GiveFeedback(object sender, GiveFeedbackEventArgs e)
		{
			try {
				e.UseDefaultCursors = true;
				e.Handled = true;
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		void textArea_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
		{
			try {
				if (e.EscapePressed) {
					e.Action = DragAction.Cancel;
				} else if ((e.KeyStates & DragDropKeyStates.LeftMouseButton) != DragDropKeyStates.LeftMouseButton) {
					e.Action = DragAction.Drop;
				} else {
					e.Action = DragAction.Continue;
				}
				e.Handled = true;
			} catch (Exception ex) {
				OnDragException(ex);
			}
		}
		#endregion

		#region Start Drag
		object currentDragDescriptor;

		void StartDrag()
		{
			// mouse capture and Drag'n'Drop doesn't mix
			textArea.ReleaseMouseCapture();

			// prevent nested StartDrag calls
			mode = MouseSelectionMode.Drag;

			DataObject dataObject = textArea.Selection.CreateDataObject(textArea);

			DragDropEffects allowedEffects = DragDropEffects.All;
			var deleteOnMove = textArea.Selection.Segments.Select(s => new AnchorSegment(textArea.Document, s)).ToList();
			foreach (ISegment s in deleteOnMove) {
				ISegment[] result = textArea.GetDeletableSegments(s);
				if (result.Length != 1 || result[0].Offset != s.Offset || result[0].EndOffset != s.EndOffset) {
					allowedEffects &= ~DragDropEffects.Move;
				}
			}

			var copyingEventArgs = new DataObjectCopyingEventArgs(dataObject, true);
			textArea.RaiseEvent(copyingEventArgs);
			if (copyingEventArgs.CommandCancelled)
				return;

			object dragDescriptor = new object();
			this.currentDragDescriptor = dragDescriptor;

			DragDropEffects resultEffect;
			using (textArea.AllowCaretOutsideSelection()) {
				var oldCaretPosition = textArea.Caret.Position;
				try {
					Debug.WriteLine("DoDragDrop with allowedEffects=" + allowedEffects);
					resultEffect = DragDrop.DoDragDrop(textArea, dataObject, allowedEffects);
					Debug.WriteLine("DoDragDrop done, resultEffect=" + resultEffect);
				} catch (COMException ex) {
					// ignore COM errors - don't crash on badly implemented drop targets
					Debug.WriteLine("DoDragDrop failed: " + ex.ToString());
					return;
				}
				if (resultEffect == DragDropEffects.None) {
					// reset caret if drag was aborted
					textArea.Caret.Position = oldCaretPosition;
				}
			}

			this.currentDragDescriptor = null;

			if (deleteOnMove != null && resultEffect == DragDropEffects.Move && (allowedEffects & DragDropEffects.Move) == DragDropEffects.Move) {
				bool draggedInsideSingleDocument = (dragDescriptor == textArea.Document.UndoStack.LastGroupDescriptor);
				if (draggedInsideSingleDocument)
					textArea.Document.UndoStack.StartContinuedUndoGroup(null);
				textArea.Document.BeginUpdate();
				try {
					foreach (ISegment s in deleteOnMove) {
						textArea.Document.Remove(s.Offset, s.Length);
					}
				} finally {
					textArea.Document.EndUpdate();
					if (draggedInsideSingleDocument)
						textArea.Document.UndoStack.EndUndoGroup();
				}
			}
		}
		#endregion

		#region QueryCursor
		// provide the IBeam Cursor for the text area
		void textArea_QueryCursor(object sender, QueryCursorEventArgs e)
		{
			if (!e.Handled) {
				if (mode != MouseSelectionMode.None) {
					// during selection, use IBeam cursor even outside the text area
					e.Cursor = Cursors.IBeam;
					e.Handled = true;
				} else if (textArea.TextView.VisualLinesValid) {
					// Only query the cursor if the visual lines are valid.
					// If they are invalid, the cursor will get re-queried when the visual lines
					// get refreshed.
					Point p = e.GetPosition(textArea.TextView);
					if (p.X >= 0 && p.Y >= 0 && p.X <= textArea.TextView.ActualWidth && p.Y <= textArea.TextView.ActualHeight) {
						int visualColumn;
						bool isAtEndOfLine;
						int offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
						if (enableTextDragDrop && textArea.Selection.Contains(offset))
							e.Cursor = Cursors.Arrow;
						else
							e.Cursor = Cursors.IBeam;
						e.Handled = true;
					}
				}
			}
		}
		#endregion

		#region LeftButtonDown
		void textArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			mode = MouseSelectionMode.None;
			if (textArea.Document == null) {
				// Avoid entering any selection mode when there's no document attached.
				return;
			}
			if (!e.Handled && e.ChangedButton == MouseButton.Left) {
				ModifierKeys modifiers = Keyboard.Modifiers;
				bool shift = (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
				if (enableTextDragDrop && e.ClickCount == 1 && !shift) {
					int visualColumn;
					bool isAtEndOfLine;
					int offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
					if (textArea.Selection.Contains(offset)) {
						if (textArea.CaptureMouse()) {
							mode = MouseSelectionMode.PossibleDragStart;
							possibleDragStartMousePos = e.GetPosition(textArea);
						}
						e.Handled = true;
						return;
					}
				}

				var oldPosition = textArea.Caret.Position;
				SetCaretOffsetToMousePosition(e);


				if (!shift) {
					textArea.ClearSelection();
				}
				if (textArea.CaptureMouse()) {
					if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && textArea.Options.EnableRectangularSelection) {
						mode = MouseSelectionMode.Rectangular;
						if (shift && textArea.Selection is RectangleSelection) {
							textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
						}
					} else if (e.ClickCount == 1 && ((modifiers & ModifierKeys.Control) == 0)) {
						mode = MouseSelectionMode.Normal;
						if (shift && !(textArea.Selection is RectangleSelection)) {
							textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
						}
					} else {
						SimpleSegment startWord;
						if (e.ClickCount == 3) {
							mode = MouseSelectionMode.WholeLine;
							startWord = GetLineAtMousePosition(e);
						} else {
							mode = MouseSelectionMode.WholeWord;
							startWord = GetWordAtMousePosition(e);
						}
						if (startWord == SimpleSegment.Invalid) {
							mode = MouseSelectionMode.None;
							textArea.ReleaseMouseCapture();
							return;
						}
						if (shift && !textArea.Selection.IsEmpty) {
							if (startWord.Offset < textArea.Selection.SurroundingSegment.Offset) {
								textArea.Selection = textArea.Selection.SetEndpoint(new TextViewPosition(textArea.Document.GetLocation(startWord.Offset)));
							} else if (startWord.EndOffset > textArea.Selection.SurroundingSegment.EndOffset) {
								textArea.Selection = textArea.Selection.SetEndpoint(new TextViewPosition(textArea.Document.GetLocation(startWord.EndOffset)));
							}
							this.startWord = new AnchorSegment(textArea.Document, textArea.Selection.SurroundingSegment);
						} else {
							textArea.Selection = Selection.Create(textArea, startWord.Offset, startWord.EndOffset);
							this.startWord = new AnchorSegment(textArea.Document, startWord.Offset, startWord.Length);
						}
					}
				}
			}
			e.Handled = true;
		}

		public MouseSelectionMode MouseSelectionMode {
			get { return mode; }
			set {
				if (mode == value)
					return;
				if (value == MouseSelectionMode.None) {
					mode = MouseSelectionMode.None;
					textArea.ReleaseMouseCapture();
				} else if (textArea.CaptureMouse()) {
					switch (value) {
						case MouseSelectionMode.Normal:
						case MouseSelectionMode.Rectangular:
							mode = value;
							break;
						default:
							throw new NotImplementedException("Programmatically starting mouse selection is only supported for normal and rectangular selections.");
					}
				}
			}
		}
		#endregion

		#region Mouse Position <-> Text coordinates
		SimpleSegment GetWordAtMousePosition(MouseEventArgs e)
		{
			TextView textView = textArea.TextView;
			if (textView == null) return SimpleSegment.Invalid;
			Point pos = e.GetPosition(textView);
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				int visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);
				int wordStartVC = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
				if (wordStartVC == -1)
					wordStartVC = 0;
				int wordEndVC = line.GetNextCaretPosition(wordStartVC, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);
				if (wordEndVC == -1)
					wordEndVC = line.VisualLength;
				int relOffset = line.FirstDocumentLine.Offset;
				int wordStartOffset = line.GetRelativeOffset(wordStartVC) + relOffset;
				int wordEndOffset = line.GetRelativeOffset(wordEndVC) + relOffset;
				return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
			} else {
				return SimpleSegment.Invalid;
			}
		}

		SimpleSegment GetLineAtMousePosition(MouseEventArgs e)
		{
			TextView textView = textArea.TextView;
			if (textView == null) return SimpleSegment.Invalid;
			Point pos = e.GetPosition(textView);
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				return new SimpleSegment(line.StartOffset, line.LastDocumentLine.EndOffset - line.StartOffset);
			} else {
				return SimpleSegment.Invalid;
			}
		}

		int GetOffsetFromMousePosition(MouseEventArgs e, out int visualColumn, out bool isAtEndOfLine)
		{
			return GetOffsetFromMousePosition(e.GetPosition(textArea.TextView), out visualColumn, out isAtEndOfLine);
		}

		int GetOffsetFromMousePosition(Point positionRelativeToTextView, out int visualColumn, out bool isAtEndOfLine)
		{
			visualColumn = 0;
			TextView textView = textArea.TextView;
			Point pos = positionRelativeToTextView;
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			if (pos.Y >= textView.DocumentHeight)
				pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace, out isAtEndOfLine);
				return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
			}
			isAtEndOfLine = false;
			return -1;
		}

		int GetOffsetFromMousePositionFirstTextLineOnly(Point positionRelativeToTextView, out int visualColumn)
		{
			visualColumn = 0;
			TextView textView = textArea.TextView;
			Point pos = positionRelativeToTextView;
			if (pos.Y < 0)
				pos.Y = 0;
			if (pos.Y > textView.ActualHeight)
				pos.Y = textView.ActualHeight;
			pos += textView.ScrollOffset;
			if (pos.Y >= textView.DocumentHeight)
				pos.Y = textView.DocumentHeight - ExtensionMethods.Epsilon;
			VisualLine line = textView.GetVisualLineFromVisualTop(pos.Y);
			if (line != null) {
				visualColumn = line.GetVisualColumn(line.TextLines.First(), pos.X, textArea.Selection.EnableVirtualSpace);
				return line.GetRelativeOffset(visualColumn) + line.FirstDocumentLine.Offset;
			}
			return -1;
		}
		#endregion

		#region MouseMove
		void textArea_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.Handled)
				return;
			if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine || mode == MouseSelectionMode.Rectangular) {
				e.Handled = true;
				if (textArea.TextView.VisualLinesValid) {
					// If the visual lines are not valid, don't extend the selection.
					// Extending the selection forces a VisualLine refresh, and it is sufficient
					// to do that on MouseUp, we don't have to do it every MouseMove.
					ExtendSelectionToMouse(e);
				}
			} else if (mode == MouseSelectionMode.PossibleDragStart) {
				e.Handled = true;
				Vector mouseMovement = e.GetPosition(textArea) - possibleDragStartMousePos;
				if (Math.Abs(mouseMovement.X) > SystemParameters.MinimumHorizontalDragDistance
					|| Math.Abs(mouseMovement.Y) > SystemParameters.MinimumVerticalDragDistance) {
					StartDrag();
				}
			}
		}
		#endregion

		#region ExtendSelection
		void SetCaretOffsetToMousePosition(MouseEventArgs e)
		{
			SetCaretOffsetToMousePosition(e, null);
		}

		void SetCaretOffsetToMousePosition(MouseEventArgs e, ISegment allowedSegment)
		{
			int visualColumn;
			bool isAtEndOfLine;
			int offset;
			if (mode == MouseSelectionMode.Rectangular) {
				offset = GetOffsetFromMousePositionFirstTextLineOnly(e.GetPosition(textArea.TextView), out visualColumn);
				isAtEndOfLine = true;
			} else {
				offset = GetOffsetFromMousePosition(e, out visualColumn, out isAtEndOfLine);
			}
			if (allowedSegment != null) {
				offset = offset.CoerceValue(allowedSegment.Offset, allowedSegment.EndOffset);
			}
			if (offset >= 0) {
				textArea.Caret.Position = new TextViewPosition(textArea.Document.GetLocation(offset), visualColumn) { IsAtEndOfLine = isAtEndOfLine };
				textArea.Caret.DesiredXPos = double.NaN;
			}
		}

		void ExtendSelectionToMouse(MouseEventArgs e)
		{
			TextViewPosition oldPosition = textArea.Caret.Position;
			if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.Rectangular) {
				SetCaretOffsetToMousePosition(e);
				if (mode == MouseSelectionMode.Normal && textArea.Selection is RectangleSelection)
					textArea.Selection = new SimpleSelection(textArea, oldPosition, textArea.Caret.Position);
				else if (mode == MouseSelectionMode.Rectangular && !(textArea.Selection is RectangleSelection))
					textArea.Selection = new RectangleSelection(textArea, oldPosition, textArea.Caret.Position);
				else
					textArea.Selection = textArea.Selection.StartSelectionOrSetEndpoint(oldPosition, textArea.Caret.Position);
			} else if (mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine) {
				var newWord = (mode == MouseSelectionMode.WholeLine) ? GetLineAtMousePosition(e) : GetWordAtMousePosition(e);
				if (newWord != SimpleSegment.Invalid) {
					textArea.Selection = Selection.Create(textArea,
														  Math.Min(newWord.Offset, startWord.Offset),
														  Math.Max(newWord.EndOffset, startWord.EndOffset));
					// moves caret to start or end of selection
					if (newWord.Offset < startWord.Offset)
						textArea.Caret.Offset = newWord.Offset;
					else
						textArea.Caret.Offset = Math.Max(newWord.EndOffset, startWord.EndOffset);
				}
			}
			textArea.Caret.BringCaretToView(5.0);
		}
		#endregion

		#region MouseLeftButtonUp
		void textArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			if (mode == MouseSelectionMode.None || e.Handled)
				return;
			e.Handled = true;
			if (mode == MouseSelectionMode.PossibleDragStart) {
				// -> this was not a drag start (mouse didn't move after mousedown)
				SetCaretOffsetToMousePosition(e);
				textArea.ClearSelection();
			} else if (mode == MouseSelectionMode.Normal || mode == MouseSelectionMode.WholeWord || mode == MouseSelectionMode.WholeLine || mode == MouseSelectionMode.Rectangular) {
				ExtendSelectionToMouse(e);
			}
			mode = MouseSelectionMode.None;
			textArea.ReleaseMouseCapture();
		}
		#endregion
	}
}
