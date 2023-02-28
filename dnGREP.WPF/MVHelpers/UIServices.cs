using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace dnGREP.WPF.MVHelpers
{
    /// <summary>
    ///   Contains helper methods for UI, so far just one for showing a waitcursor
    ///   http://stackoverflow.com/questions/7346663/how-to-show-a-waitcursor-when-the-wpf-application-is-busy-databinding
    /// </summary>
    public static class UIServices
    {
        /// <summary>
        ///   A value indicating whether the UI is currently busy
        /// </summary>
        private static bool IsBusy;
        private static DispatcherTimer? timer;

        /// <summary>
        /// Sets the busy state as busy.
        /// </summary>
        public static void SetBusyState()
        {
            SetBusyState(true);
        }

        /// <summary>
        /// Sets the busy state to busy or not busy.
        /// </summary>
        /// <param name="busy">if set to <c>true</c> the application is now busy.</param>
        private static void SetBusyState(bool busy)
        {
            if (busy != IsBusy)
            {
                IsBusy = busy;
                Mouse.OverrideCursor = busy ? Cursors.Wait : null;

                if (IsBusy)
                {
                    timer = new(TimeSpan.FromSeconds(0), DispatcherPriority.ApplicationIdle, DispatcherTimer_Tick, Dispatcher.CurrentDispatcher);
                    timer.Start();
                }
            }
        }

        /// <summary>
        /// Handles the Tick event of the dispatcherTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void DispatcherTimer_Tick(object? sender, EventArgs e)
        {
            if (sender is DispatcherTimer dispatcherTimer)
            {
                SetBusyState(false);
                dispatcherTimer.Stop();
            }
        }
    }
}
