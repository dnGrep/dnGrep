using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for BusyIndicator.xaml
    /// </summary>
    public partial class BusyIndicator : UserControl
    {
        private delegate void VoidDelegete();
        private readonly DispatcherTimer timer = new();

        public BusyIndicator()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (Visibility != Visibility.Visible)
                timer.Stop();

            rotationCanvas.Dispatcher.Invoke
            (
                new VoidDelegete(
                    delegate
                    {
                        SpinnerRotate.Angle += 30;
                        if (SpinnerRotate.Angle == 360)
                        {
                            SpinnerRotate.Angle = 0;
                        }
                    }
                    ),
                null
            );
        }
    }
}
