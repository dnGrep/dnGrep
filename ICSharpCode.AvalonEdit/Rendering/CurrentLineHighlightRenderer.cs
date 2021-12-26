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
using System.Windows;
using System.Windows.Media;

namespace ICSharpCode.AvalonEdit.Rendering
{
	sealed class CurrentLineHighlightRenderer : IBackgroundRenderer
	{
		#region Fields

		int line;
		TextView textView;

		public static readonly Color DefaultBackground = Color.FromArgb(22, 20, 220, 224);
		public static readonly Color DefaultBorder = Color.FromArgb(52, 0, 255, 110);

		#endregion

		#region Properties

		public int Line {
			get { return this.line; }
			set {
				if (this.line != value) {
					this.line = value;
					this.textView.InvalidateLayer(this.Layer);
				}
			}
		}

		public KnownLayer Layer {
			get { return KnownLayer.Selection; }
		}

		public Brush BackgroundBrush {
			get; set;
		}

		public Pen BorderPen {
			get; set;
		}

		#endregion

		public CurrentLineHighlightRenderer(TextView textView)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");

			this.BorderPen = new Pen(new SolidColorBrush(DefaultBorder), 1);
			this.BorderPen.Freeze();

			this.BackgroundBrush = new SolidColorBrush(DefaultBackground);
			this.BackgroundBrush.Freeze();

			this.textView = textView;
			this.textView.BackgroundRenderers.Add(this);

			this.line = 0;
		}

		public void Draw(TextView textView, DrawingContext drawingContext)
		{
			if (!this.textView.Options.HighlightCurrentLine)
				return;

			BackgroundGeometryBuilder builder = new BackgroundGeometryBuilder();

			var visualLine = this.textView.GetVisualLine(line);
			if (visualLine == null) return;

			var linePosY = visualLine.VisualTop - this.textView.ScrollOffset.Y;

			builder.AddRectangle(textView, new Rect(0, linePosY, textView.ActualWidth, visualLine.Height));

			Geometry geometry = builder.CreateGeometry();
			if (geometry != null) {
				drawingContext.DrawGeometry(this.BackgroundBrush, this.BorderPen, geometry);
			}
		}
	}
}
