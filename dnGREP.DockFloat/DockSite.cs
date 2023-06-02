using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using dnGREP.Common.UI;

namespace dnGREP.DockFloat
{
    /// <summary> 
    ///   Use this as a container for the UI elements that will be docked. Note,
    ///   this class has nothing to do with <see
    ///   cref="System.Windows.Controls.Dock"/> or WPF's <see cref="DockPanel"/>
    ///   class.
    /// </summary>
    [ContentProperty("Content")]
    [TemplatePart(Name = "PART_PopOutButton", Type = typeof(ButtonBase))]
    public class DockSite : Control
    {
        private Window? floatingWindow;
        private ContentState? savedContentState;

        static DockSite()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockSite), new FrameworkPropertyMetadata(typeof(DockSite)));

            if (RunninginXamlDesigner) return;

            Application.Current.MainWindow.StateChanged += MinimizeOrRestoreWithMainWindow;
            Application.Current.MainWindow.Closing += MainWindow_Closing;
        }

        private static bool mainWindowClosing;
        private static void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (sender is Window mainWindow)
            {
                mainWindowClosing = true;
                var floatWindows = GetAllFloatWindows(mainWindow);
                foreach (var window in floatWindows)
                {
                    if (window is FloatWindow floatWindow)
                    {
                        floatWindow.ForceClose();
                    }
                }
            }
        }

        public static bool RunninginXamlDesigner { get; } =
            DesignerProperties.GetIsInDesignMode(new DependencyObject());

        static void MinimizeOrRestoreWithMainWindow(object? sender, EventArgs e)
        {
            if (sender is Window mainWindow)
            {
                var floatWindows = GetAllFloatWindows(mainWindow);
                foreach (var floatWindow in floatWindows)
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                        floatWindow.WindowState = WindowState.Minimized;
                    else if (mainWindow.WindowState == WindowState.Normal)
                        floatWindow.WindowState = WindowState.Normal;
                }
                mainWindow.Activate();
            }
        }

        public static IEnumerable<Window> GetAllFloatWindows(Window mainWindow) =>
            from dockSite in mainWindow.FindLogicalChildren<DockSite>()
            where dockSite.floatingWindow != null
            select dockSite.floatingWindow;

        public FrameworkElement? Content
        {
            get => (FrameworkElement)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(FrameworkElement), typeof(DockSite),
                new PropertyMetadata(null));

        public bool ButtonOverlapsContent
        {
            get => (bool)GetValue(ButtonOverlapsContentProperty);
            set => SetValue(ButtonOverlapsContentProperty, value);
        }
        public static readonly DependencyProperty ButtonOverlapsContentProperty =
            DependencyProperty.Register("ButtonOverlapsContent", typeof(bool), typeof(DockSite), new PropertyMetadata(true));

        public bool ShowHeader
        {
            get => (bool)GetValue(ShowHeaderProperty);
            set => SetValue(ShowHeaderProperty, value);
        }
        public static readonly DependencyProperty ShowHeaderProperty =
            DependencyProperty.Register("ShowHeader", typeof(bool), typeof(DockSite), new PropertyMetadata(false));

        public string Heading
        {
            get => (string)GetValue(HeadingProperty);
            set => SetValue(HeadingProperty, value);
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(string), typeof(DockSite), new PropertyMetadata(string.Empty));

        public Rect FloatWindowBounds
        {
            get => (Rect)GetValue(FloatWindowBoundsProperty);
            set => SetValue(FloatWindowBoundsProperty, value);
        }
        public static readonly DependencyProperty FloatWindowBoundsProperty =
            DependencyProperty.Register("FloatWindowBounds", typeof(Rect), typeof(DockSite), new PropertyMetadata(Rect.Empty));

        public WindowState FloatWindowState
        {
            get => (WindowState)GetValue(FloatWindowStateProperty);
            set => SetValue(FloatWindowStateProperty, value);
        }
        public static readonly DependencyProperty FloatWindowStateProperty =
            DependencyProperty.Register("FloatWindowState", typeof(WindowState), typeof(DockSite), new PropertyMetadata(WindowState.Normal));

        public bool IsDocked
        {
            get => (bool)GetValue(IsDockedProperty);
            set => SetValue(IsDockedProperty, value);
        }
        public static readonly DependencyProperty IsDockedProperty =
            DependencyProperty.Register("IsDocked", typeof(bool), typeof(DockSite), new PropertyMetadata(true));

        public bool IsHidden
        {
            get => (bool)GetValue(IsHiddenProperty);
            set => SetValue(IsHiddenProperty, value);
        }
        public static readonly DependencyProperty IsHiddenProperty =
            DependencyProperty.Register("IsHidden", typeof(bool), typeof(DockSite), new PropertyMetadata(false));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("PART_PopOutButton") is Button popOutButton)
            {
                popOutButton.Click += (s, e) => PopOut(false);
            }
        }

        public static void InitFloatingWindows()
        {
            var sites = Application.Current.MainWindow.FindLogicalChildren<DockSite>()
                .Where(d => !d.IsDocked).ToArray();

            foreach (var dockSite in sites)
                dockSite.PopOut(dockSite.IsHidden);
        }

        private void SavePlacement()
        {
            if (floatingWindow == null) return;

            FloatWindowBounds = new Rect(
                floatingWindow.Left, floatingWindow.Top,
                floatingWindow.ActualWidth, floatingWindow.ActualHeight);

            FloatWindowState = floatingWindow.WindowState == WindowState.Maximized ?
                WindowState.Maximized : WindowState.Normal;

            IsHidden = floatingWindow.Visibility != Visibility.Visible;
        }

        void PopOut(bool hidden)
        {
            IsDocked = false;
            SaveContentFromDock();
            AddContentToNewFloatingWindow(hidden);
        }

        void DockIn()
        {
            BindingOperations.ClearAllBindings(floatingWindow);
            floatingWindow = null;
            RestoreContentToDock();

            if (!mainWindowClosing)
                IsDocked = true;
        }

        void SaveContentFromDock()
        {
            if (Content != null)
            {
                savedContentState = ContentState.Save(Content);
            }
            else
            {
                savedContentState = null;
            }
            Content = null;
        }

        void RestoreContentToDock()
        {
            if (savedContentState != null)
            {
                Content = savedContentState.Restore();
            }
            savedContentState = null;
        }

        void AddContentToNewFloatingWindow(bool hidden)
        {
            if (savedContentState == null) return;

            if (FloatWindowBounds == Rect.Empty || FloatWindowBounds == new Rect(0, 0, 0, 0))
            {
                FloatWindowBounds = GetPopupPosition();
            }

            floatingWindow = new FloatWindow(savedContentState.FloatContent)
            {
                DataContext = DataContext,
                Background = Background,
                Left = FloatWindowBounds.Left,
                Top = FloatWindowBounds.Top,
                Width = FloatWindowBounds.Width,
                Height = FloatWindowBounds.Height,
                WindowState = FloatWindowState,
            };
            BindingOperations.SetBinding(floatingWindow, Window.TitleProperty, new Binding("Heading") { Source = this });
            BindingOperations.SetBinding(floatingWindow, Window.FontSizeProperty, new Binding("FontSize") { Source = this });

            floatingWindow.Loaded += (s, e) =>
            {
                if (!floatingWindow.IsOnScreen())
                    floatingWindow.ToRightEdge();

                floatingWindow.ConstrainToScreen();
            };
            floatingWindow.IsVisibleChanged += (s, e) => SavePlacement();
            floatingWindow.Closing += (s, e) => SavePlacement();
            floatingWindow.Closed += (s, e) => DockIn();
            if (!hidden)
                floatingWindow.Show();
        }

        private static Rect GetPopupPosition()
        {
            var wind = Application.Current.MainWindow;
            var position = new Point(wind.Left + wind.ActualWidth, wind.Top);
            return new Rect(position, new Size(640, wind.Height));
        }
    }
}
