using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace dnGREP.WPF
{
	/// <summary>
	/// Interaction logic for Test.xaml
	/// </summary>
	public partial class Test : Window
	{
		public Test()
		{
			InitializeComponent();
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			cbHistory.IsDropDownOpen = true;
		}

		private void cbHistory_MouseDown(object sender, MouseButtonEventArgs e)
		{
			textBox1.Text = sender.GetType().ToString();
		}
	}
}
