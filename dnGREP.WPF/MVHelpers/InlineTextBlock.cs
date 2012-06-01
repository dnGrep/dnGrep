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

    public class RichTextBlock : System.Windows.Controls.TextBlock
    {
        public static DependencyProperty InlineProperty;

        static RichTextBlock()
        {
            //This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
            //This style is defined in themes\generic.xaml
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RichTextBlock), new FrameworkPropertyMetadata(typeof(RichTextBlock)));
            InlineProperty = DependencyProperty.Register("RichText", typeof(List<Inline>), typeof(RichTextBlock), new PropertyMetadata(null, new PropertyChangedCallback(OnInlineChanged)));
        }

        public List<Inline> RichText
        {
            get { return (List<Inline>)GetValue(InlineProperty); }
            set { SetValue(InlineProperty, value); }
        }

        public static void OnInlineChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue == e.OldValue) return;
            RichTextBlock r = sender as RichTextBlock;
            List<Inline> i = e.NewValue as List<Inline>;
            if (r == null || i == null) return;

            r.Inlines.Clear();
            foreach (Inline inline in i)
            {
                r.Inlines.Add(inline);
            }
        }
    }
}
