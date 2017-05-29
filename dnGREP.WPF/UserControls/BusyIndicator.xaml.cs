using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace dnGREP.WPF.UserControls
{
    /// <summary>
    /// Interaction logic for BusyIndicator.xaml
    /// </summary>
    public partial class BusyIndicator : UserControl
    {
        private const string PERCENTS_TEXT = "{0}%";
        private delegate void VoidDelegete();
        private Timer timer;

        public BusyIndicator()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            timer = new Timer(100);
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
        }

        void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (Visibility != System.Windows.Visibility.Visible)
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
