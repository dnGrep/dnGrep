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

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Enumeration of possible states of mouse selection.
	/// </summary>
	public enum MouseSelectionMode
	{
		/// <summary>
		/// no selection (no mouse button down)
		/// </summary>
		None,
		/// <summary>
		/// left mouse button down on selection, might be normal click
		/// or might be drag'n'drop
		/// </summary>
		PossibleDragStart,
		/// <summary>
		/// dragging text
		/// </summary>
		Drag,
		/// <summary>
		/// normal selection (click+drag)
		/// </summary>
		Normal,
		/// <summary>
		/// whole-word selection (double click+drag or ctrl+click+drag)
		/// </summary>
		WholeWord,
		/// <summary>
		/// whole-line selection (triple click+drag)
		/// </summary>
		WholeLine,
		/// <summary>
		/// rectangular selection (alt+click+drag)
		/// </summary>
		Rectangular
	}
}
