using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using dnGREP.Common.UI;

namespace dnGREP.WPF
{
    /// <summary>
    /// Interaction logic for ReplaceWindow.xaml
    /// </summary>
    public partial class ReplaceWindow : Window
    {
        private VirtualizingPanel vp;

        public ReplaceWindow()
        {
            InitializeComponent();

            DataContext = ViewModel;

            ViewModel.LineChanged += ViewModel_LineChanged;
            ViewModel.CloseTrue += ViewModel_CloseTrue;

            Loaded += (s, e) => vp = lineList.FindVisualChildren<VirtualizingPanel>().FirstOrDefault();
        }

        public ReplaceViewModel ViewModel { get; } = new ReplaceViewModel();


        private void ViewModel_LineChanged(object sender, LineChangedEventArgs e)
        {
            if (vp != null)
            {
                Dispatcher.Invoke(() =>
                {
                    //int index = lineList.Items.IndexOf(e.LineIndex);
                    vp.BringIndexIntoViewPublic(e.LineIndex);

                }, System.Windows.Threading.DispatcherPriority.Render);
            }
            else
            {
            }
        }

        private void ViewModel_CloseTrue(object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
