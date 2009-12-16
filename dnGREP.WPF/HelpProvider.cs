using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace dnGREP.WPF
{
	static class HelpProvider
	{
		public static readonly DependencyProperty HelpStringProperty =
		DependencyProperty.RegisterAttached("HelpString", typeof(string), typeof(HelpProvider));

		static HelpProvider()
		{
			CommandManager.RegisterClassCommandBinding(typeof(FrameworkElement), new
			CommandBinding(ApplicationCommands.Help, new ExecutedRoutedEventHandler
			(Executed), new CanExecuteRoutedEventHandler(CanExecute)));
		}

		public static void SetHelpString(DependencyObject obj, string value)
		{
			obj.SetValue(HelpStringProperty, value);
		}


		static private void CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			FrameworkElement senderElement = sender as FrameworkElement;
			if (HelpProvider.GetHelpString(senderElement) != null)
				e.CanExecute = true;
		}

		public static string GetHelpString(DependencyObject obj)
		{
			return (string)obj.GetValue(HelpStringProperty);
		}

		static private void Executed(object sender, ExecutedRoutedEventArgs e)
		{
			System.Windows.Forms.MessageBox.Show("Help: " + HelpProvider.GetHelpString(sender
			as FrameworkElement));
		}
	}  
}
