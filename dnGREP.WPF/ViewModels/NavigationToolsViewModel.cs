using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class NavigationToolsViewModel : CultureAwareViewModel
    {
        public static NavigationToolsViewModel Instance { get; private set; } = new();
        private ToolSize navToolsSize;

        private NavigationToolsViewModel()
        {
            double fontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.MainFormFontSize);
            navToolsSize = GrepSettings.Instance.Get<ToolSize>(GrepSettings.Key.NavToolsSize);
            ChangeNavigationToolsSize(fontSize, navToolsSize);

            NavigationButtonsVisible = GrepSettings.Instance.Get<bool>(GrepSettings.Key.NavigationButtonsVisible);
            NavigationToolsPosition = GrepSettings.Instance.Get<NavigationToolsPosition>(GrepSettings.Key.NavToolsPosition);

            OnNavigationToolsPositionChanged(NavigationToolsPosition);
        }

        internal void ChangeNavigationToolsSize(double fontSize, ToolSize navToolsSize)
        {
            this.navToolsSize = navToolsSize;
            ButtonFontSize = navToolsSize switch
            {
                ToolSize.Large => fontSize + 8,
                ToolSize.Medium => fontSize + 4,
                _ => fontSize,
            };
            OnNavigationToolsPositionChanged(NavigationToolsPosition);
        }

        internal void ChangeNavigationToolsPosition(bool visible, NavigationToolsPosition position)
        {
            NavigationButtonsVisible = visible;
            NavigationToolsPosition = position;
        }

        partial void OnNavigationToolsPositionChanged(NavigationToolsPosition value)
        {
            AbovePanelVisible = value switch
            {
                NavigationToolsPosition.Above => NavigationButtonsVisible,
                _ => false,
            };

            TreePanelVisible = value switch
            {
                NavigationToolsPosition.Above => false,
                _ => NavigationButtonsVisible,
            };

            BorderDock = value switch
            {
                NavigationToolsPosition.TopLeft => Dock.Top,
                NavigationToolsPosition.TopCenter => Dock.Top,
                NavigationToolsPosition.TopRight => Dock.Top,
                NavigationToolsPosition.BottomLeft => Dock.Bottom,
                NavigationToolsPosition.BottomCenter => Dock.Bottom,
                NavigationToolsPosition.BottomRight => Dock.Bottom,
                NavigationToolsPosition.LeftTop => Dock.Left,
                NavigationToolsPosition.LeftCenter => Dock.Left,
                NavigationToolsPosition.LeftBottom => Dock.Left,
                NavigationToolsPosition.RightTop => Dock.Right,
                NavigationToolsPosition.RightCenter => Dock.Right,
                NavigationToolsPosition.RightBottom => Dock.Right,
                _ => Dock.Top,
            };

            BorderThickness = value switch
            {
                NavigationToolsPosition.TopLeft => new(1, 1, 1, 0),
                NavigationToolsPosition.TopCenter => new(1, 1, 1, 0),
                NavigationToolsPosition.TopRight => new(1, 1, 1, 0),
                NavigationToolsPosition.BottomLeft => new(1, 0, 1, 1),
                NavigationToolsPosition.BottomCenter => new(1, 0, 1, 1),
                NavigationToolsPosition.BottomRight => new(1, 0, 1, 1),
                NavigationToolsPosition.LeftTop => new(1, 1, 0, 1),
                NavigationToolsPosition.LeftCenter => new(1, 1, 0, 1),
                NavigationToolsPosition.LeftBottom => new(1, 1, 0, 1),
                NavigationToolsPosition.RightTop => new(0, 1, 1, 1),
                NavigationToolsPosition.RightCenter => new(0, 1, 1, 1),
                NavigationToolsPosition.RightBottom => new(0, 1, 1, 1),
                _ => new(0, 0, 0, 0),
            };

            ButtonOrientation = value switch
            {
                NavigationToolsPosition.TopLeft => Orientation.Horizontal,
                NavigationToolsPosition.TopCenter => Orientation.Horizontal,
                NavigationToolsPosition.TopRight => Orientation.Horizontal,
                NavigationToolsPosition.BottomLeft => Orientation.Horizontal,
                NavigationToolsPosition.BottomCenter => Orientation.Horizontal,
                NavigationToolsPosition.BottomRight => Orientation.Horizontal,
                NavigationToolsPosition.LeftTop => Orientation.Vertical,
                NavigationToolsPosition.LeftCenter => Orientation.Vertical,
                NavigationToolsPosition.LeftBottom => Orientation.Vertical,
                NavigationToolsPosition.RightTop => Orientation.Vertical,
                NavigationToolsPosition.RightCenter => Orientation.Vertical,
                NavigationToolsPosition.RightBottom => Orientation.Vertical,
                _ => Orientation.Horizontal,
            };

            ButtonHorizontalAlignment = value switch
            {
                NavigationToolsPosition.TopLeft => HorizontalAlignment.Left,
                NavigationToolsPosition.TopCenter => HorizontalAlignment.Center,
                NavigationToolsPosition.TopRight => HorizontalAlignment.Right,
                NavigationToolsPosition.BottomLeft => HorizontalAlignment.Left,
                NavigationToolsPosition.BottomCenter => HorizontalAlignment.Center,
                NavigationToolsPosition.BottomRight => HorizontalAlignment.Right,
                NavigationToolsPosition.LeftTop => HorizontalAlignment.Stretch,
                NavigationToolsPosition.LeftCenter => HorizontalAlignment.Stretch,
                NavigationToolsPosition.LeftBottom => HorizontalAlignment.Stretch,
                NavigationToolsPosition.RightTop => HorizontalAlignment.Stretch,
                NavigationToolsPosition.RightCenter => HorizontalAlignment.Stretch,
                NavigationToolsPosition.RightBottom => HorizontalAlignment.Stretch,
                _ => HorizontalAlignment.Left,
            };

            ButtonVerticalAlignment = value switch
            {
                NavigationToolsPosition.TopLeft => VerticalAlignment.Stretch,
                NavigationToolsPosition.TopCenter => VerticalAlignment.Stretch,
                NavigationToolsPosition.TopRight => VerticalAlignment.Stretch,
                NavigationToolsPosition.BottomLeft => VerticalAlignment.Stretch,
                NavigationToolsPosition.BottomCenter => VerticalAlignment.Stretch,
                NavigationToolsPosition.BottomRight => VerticalAlignment.Stretch,
                NavigationToolsPosition.LeftTop => VerticalAlignment.Top,
                NavigationToolsPosition.LeftCenter => VerticalAlignment.Center,
                NavigationToolsPosition.LeftBottom => VerticalAlignment.Bottom,
                NavigationToolsPosition.RightTop => VerticalAlignment.Top,
                NavigationToolsPosition.RightCenter => VerticalAlignment.Center,
                NavigationToolsPosition.RightBottom => VerticalAlignment.Bottom,
                _ => VerticalAlignment.Stretch,
            };

            ButtonMargin = value switch
            {
                NavigationToolsPosition.TopLeft => new(3, 1, 3, 1),
                NavigationToolsPosition.TopCenter => new(3, 1, 3, 1),
                NavigationToolsPosition.TopRight => new(3, 1, 3, 1),
                NavigationToolsPosition.BottomLeft => new(3, 1, 3, 1),
                NavigationToolsPosition.BottomCenter => new(3, 1, 3, 1),
                NavigationToolsPosition.BottomRight => new(3, 1, 3, 1),
                NavigationToolsPosition.LeftTop => new(1, 3, 1, 3),
                NavigationToolsPosition.LeftCenter => new(1, 3, 1, 3),
                NavigationToolsPosition.LeftBottom => new(1, 3, 1, 3),
                NavigationToolsPosition.RightTop => new(1, 3, 1, 3),
                NavigationToolsPosition.RightCenter => new(1, 3, 1, 3),
                NavigationToolsPosition.RightBottom => new(1, 3, 1, 3),
                _ => new(3, 1, 3, 1),
            };

            double sz = navToolsSize switch
            {
                ToolSize.Large => 6,
                ToolSize.Medium => 4,
                _ => 3,
            };
            ButtonPadding = value switch
            {
                NavigationToolsPosition.TopLeft => new(sz, 1, sz, 1),
                NavigationToolsPosition.TopCenter => new(sz, 1, sz, 1),
                NavigationToolsPosition.TopRight => new(sz, 1, sz, 1),
                NavigationToolsPosition.BottomLeft => new(sz, 1, sz, 1),
                NavigationToolsPosition.BottomCenter => new(sz, 1, sz, 1),
                NavigationToolsPosition.BottomRight => new(sz, 1, sz, 1),
                NavigationToolsPosition.LeftTop => new(3, sz, 3, sz),
                NavigationToolsPosition.LeftCenter => new(3, sz, 3, sz),
                NavigationToolsPosition.LeftBottom => new(3, sz, 3, sz),
                NavigationToolsPosition.RightTop => new(3, sz, 3, sz),
                NavigationToolsPosition.RightCenter => new(3, sz, 3, sz),
                NavigationToolsPosition.RightBottom => new(3, sz, 3, sz),
                _ => new(sz, 1, sz, 1),
            };
        }

        [ObservableProperty]
        private bool navigationButtonsVisible = true;

        [ObservableProperty]
        private NavigationToolsPosition navigationToolsPosition = NavigationToolsPosition.LeftTop;

        [ObservableProperty]
        private bool abovePanelVisible = false;

        [ObservableProperty]
        private bool treePanelVisible = true;

        [ObservableProperty]
        private Dock borderDock = Dock.Left;

        [ObservableProperty]
        private Thickness borderThickness = new(1, 1, 0, 1);

        [ObservableProperty]
        private Orientation buttonOrientation = Orientation.Vertical;

        [ObservableProperty]
        private HorizontalAlignment buttonHorizontalAlignment = HorizontalAlignment.Stretch;

        [ObservableProperty]
        private VerticalAlignment buttonVerticalAlignment = VerticalAlignment.Top;

        [ObservableProperty]
        private Thickness buttonMargin = new(1, 3, 1, 3);

        [ObservableProperty]
        private Thickness buttonPadding = new(3, 6, 3, 6);

        [ObservableProperty]
        private double buttonFontSize = 20;

        private static MainForm? MainForm => Application.Current.MainWindow as MainForm;

        private RelayCommand? previousFileCommand;
        public RelayCommand PreviousFileCommand => previousFileCommand ??= new RelayCommand(
            p => MainForm?.PreviousFile(),
            q => MainForm?.HasSearchResults ?? false);

        private RelayCommand? previousMatchCommand;
        public RelayCommand PreviousMatchCommand => previousMatchCommand ??= new RelayCommand(
            p => MainForm?.PreviousMatch(),
            q => MainForm?.HasSearchResults ?? false);

        private RelayCommand? nextMatchCommand;
        public RelayCommand NextMatchCommand => nextMatchCommand ??= new RelayCommand(
            p => MainForm?.NextMatch(),
            q => MainForm?.HasSearchResults ?? false);

        private RelayCommand? nextFileCommand;
        public RelayCommand NextFileCommand => nextFileCommand ??= new RelayCommand(
            p => MainForm?.NextFile(),
            q => MainForm?.HasSearchResults ?? false);

        private RelayCommand? scrollToCurrentCommand;
        public RelayCommand ScrollToCurrentCommand => scrollToCurrentCommand ??= new RelayCommand(
            p => MainForm?.ScrollToCurrent(),
            q => MainForm?.HasStartItem ?? false);

        private RelayCommand? collapseAllCommand;
        public RelayCommand CollapseAllCommand => collapseAllCommand ??= new RelayCommand(
            p => MainForm?.CollapseAll(),
            q => MainForm?.HasExpandedNode ?? false);
    }
}
