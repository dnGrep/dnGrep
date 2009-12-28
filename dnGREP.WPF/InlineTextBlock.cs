using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;

namespace dnGREP.WPF
{
	public class InlineTextBlock : TextBlock
	{
		public InlineCollection InlineCollection
		{
			get
			{
				return (InlineCollection)GetValue(InlineCollectionProperty);
			}
			set
			{
				SetValue(InlineCollectionProperty, value);
			}
		}

		public static readonly DependencyProperty InlineCollectionProperty = DependencyProperty.Register(
			"InlineCollection",
			typeof(InlineCollection),
			typeof(InlineTextBlock),
				new UIPropertyMetadata((PropertyChangedCallback)((sender, args) =>
				{
					InlineTextBlock textBlock = sender as InlineTextBlock;

					if (textBlock != null)
					{
						textBlock.Inlines.Clear();

						InlineCollection inlines = args.NewValue as InlineCollection;

						if (inlines != null)
							textBlock.Inlines.AddRange(inlines.ToList());
					}
				})));				
	}
}
