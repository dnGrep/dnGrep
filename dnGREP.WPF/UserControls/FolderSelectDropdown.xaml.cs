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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Specialized;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for FolderSelectDropdown.xaml
    /// </summary>
    public partial class FolderSelectDropdown : UserControl
    {
        public FolderSelectDropdown()
        {
            InitializeComponent();
        }

        private void tbFolderName_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void tbFolderName_Drop(object sender, DragEventArgs e)
        {
            if (e.Data is System.Windows.DataObject &&
            ((System.Windows.DataObject)e.Data).ContainsFileDropList())
            {
                ((MainViewModel)(this.DataContext)).FileOrFolderPath = "";
                StringCollection fileNames = ((System.Windows.DataObject)e.Data).GetFileDropList();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < fileNames.Count; i++)
                {
                    sb.Append(fileNames[i]);
                    if (i < (fileNames.Count - 1))
                        sb.Append(";");
                }
                ((MainViewModel)(this.DataContext)).FileOrFolderPath = sb.ToString();
            }
        }

        private void TextBoxFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source is TextBox)
            {
                ((TextBox)e.Source).SelectAll();
            }
        }
    }
}
