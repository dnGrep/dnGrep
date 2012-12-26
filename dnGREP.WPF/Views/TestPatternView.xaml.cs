using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using dnGREP.Common;
using dnGREP.Engines;
using NLog;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for TestPattern.xaml
    /// </summary>
    public partial class TestPattern : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
		private TestPatternViewModel inputData = new TestPatternViewModel();

        public TestPattern()
        {
            InitializeComponent();
            this.DataContext = inputData;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            inputData.UpdateState("");
        }

        private void formKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }               

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            GrepSettings.Instance.Save();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnCopyFile_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(inputData.TestOutputText);
        }
    }
}
