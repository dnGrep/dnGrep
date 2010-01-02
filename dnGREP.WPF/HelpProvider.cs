using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace dnGREP.WPF
{
    /// <summary>
    /// Source: http://social.msdn.microsoft.com/forums/en-US/wpf/thread/a7a1dfa3-29b2-48bf-a7bb-586879232f0e
    /// </summary>
    public static class DiginesisHelpProvider
    {

        #region Fields

        private static string mHelpNamespace = null;

        private static bool mShowHelp = false;

        private static string mNotFoundTopic;

        public static readonly DependencyProperty HelpStringProperty =

        DependencyProperty.RegisterAttached("HelpString", typeof(string), typeof(DiginesisHelpProvider));

        public static readonly DependencyProperty HelpKeywordProperty =

        DependencyProperty.RegisterAttached("HelpKeyword", typeof(string), typeof(DiginesisHelpProvider));

        public static readonly DependencyProperty HelpNavigatorProperty =

        DependencyProperty.RegisterAttached("HelpNavigator", typeof(HelpNavigator), typeof(DiginesisHelpProvider),

        new PropertyMetadata(HelpNavigator.TableOfContents));

        public static readonly DependencyProperty ShowHelpProperty =

        DependencyProperty.RegisterAttached("ShowHelp", typeof(bool), typeof(DiginesisHelpProvider));

        #endregion

        #region Constructors

        static DiginesisHelpProvider()
        {

            CommandManager.RegisterClassCommandBinding(

            typeof(FrameworkElement),

            new CommandBinding(ApplicationCommands.Help, OnHelpExecuted, OnHelpCanExecute));

        }

        #endregion

        #region Public Properties

        public static string HelpNamespace
        {

            get { return mHelpNamespace; }

            set { mHelpNamespace = value; }

        }



        public static bool ShowHelp
        {

            get { return mShowHelp; }

            set { mShowHelp = value; }

        }



        public static string NotFoundTopic
        {

            get { return mNotFoundTopic; }

            set { mNotFoundTopic = value; }

        }

        #endregion

        #region Public Methods

        #region HelpString

        public static string GetHelpString(DependencyObject obj)
        {

            return (string)obj.GetValue(HelpStringProperty);

        }

        public static void SetHelpString(DependencyObject obj, string value)
        {

            obj.SetValue(HelpStringProperty, value);

        }

        #endregion

        #region HelpKeyword

        public static string GetHelpKeyword(DependencyObject obj)
        {

            return (string)obj.GetValue(HelpKeywordProperty);

        }

        public static void SetHelpKeyword(DependencyObject obj, string value)
        {

            obj.SetValue(HelpKeywordProperty, value);

        }

        #endregion

        #region HelpNavigator

        public static HelpNavigator GetHelpNavigator(DependencyObject obj)
        {

            return (HelpNavigator)obj.GetValue(HelpNavigatorProperty);

        }

        public static void SetHelpNavigator(DependencyObject obj, HelpNavigator value)
        {

            obj.SetValue(HelpNavigatorProperty, value);

        }

        #endregion

        #region ShowHelp

        public static bool GetShowHelp(DependencyObject obj)
        {

            return (bool)obj.GetValue(ShowHelpProperty);

        }

        public static void SetShowHelp(DependencyObject obj, bool value)
        {

            obj.SetValue(ShowHelpProperty, value);

        }

        #endregion

        #endregion

        #region Private Members

        private static void OnHelpCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {

            e.CanExecute = CanExecuteHelp((DependencyObject)sender) || ShowHelp;

        }

        private static bool CanExecuteHelp(DependencyObject sender)
        {

            if (sender != null)
            {

                if (GetShowHelp(sender))

                    return true;

                return CanExecuteHelp(VisualTreeHelper.GetParent(sender));

            }

            return false;

        }

        private static DependencyObject GetHelp(DependencyObject sender)
        {

            if (sender != null)
            {

                if (GetShowHelp(sender))

                    return sender;

                return GetHelp(VisualTreeHelper.GetParent(sender));

            }

            return null;

        }

        private static void OnHelpExecuted(object sender, ExecutedRoutedEventArgs e)
        {

            DependencyObject ctl = GetHelp(sender as DependencyObject);

            if (ctl != null && GetShowHelp(ctl))
            {

                string caption = GetHelpString(ctl);

                string parameter = GetHelpKeyword(ctl);

                HelpNavigator command = GetHelpNavigator(ctl);

                if (Control.MouseButtons != MouseButtons.None && !string.IsNullOrEmpty(caption))
                {

                    Point point = Mouse.GetPosition(Mouse.DirectlyOver);

                    Help.ShowPopup(null, caption, Control.MousePosition);

                    e.Handled = true;

                }

                if (!e.Handled && !string.IsNullOrEmpty(HelpNamespace))
                {

                    if (!string.IsNullOrEmpty(parameter))
                    {

                        Help.ShowHelp(null, HelpNamespace, command, parameter);

                        e.Handled = true;

                    }

                    if (!e.Handled)
                    {

                        Help.ShowHelp(null, HelpNamespace, command);

                        e.Handled = true;

                    }

                }

                if (!e.Handled && !string.IsNullOrEmpty(caption))
                {

                    Point point = Mouse.GetPosition(Mouse.DirectlyOver);

                    Help.ShowPopup(null, caption, new System.Drawing.Point((int)point.X, (int)point.Y));

                    e.Handled = true;

                }

                if (!e.Handled && !string.IsNullOrEmpty(HelpNamespace))
                {

                    Help.ShowHelp(null, HelpNamespace);

                    e.Handled = true;

                }

            }

            else if (ShowHelp)
            {

                if (!string.IsNullOrEmpty(NotFoundTopic))

                    Help.ShowHelp(null, HelpNamespace, NotFoundTopic);

                else

                    Help.ShowHelp(null, HelpNamespace);

                e.Handled = true;

            }

        }

        #endregion

    }

}