using System;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.WPF.Properties;

namespace dnGREP.WPF
{
    public partial class DockViewModel : CultureAwareViewModel
    {
        public static DockViewModel Instance { get; private set; } = new();

        private DockViewModel()
        {
            IsPreviewDocked = LayoutProperties.PreviewDocked;
            if (Enum.TryParse(LayoutProperties.PreviewDockSide, out Dock side) &&
                Enum.IsDefined(side))
            {
                PreviewDockSide = side;
            }
            PreviewAutoPosition = GrepSettings.Instance.Get<bool>(GrepSettings.Key.PreviewAutoPosition);
        }

        public void SaveSettings()
        {
            LayoutProperties.PreviewDocked = IsPreviewDocked;
            LayoutProperties.PreviewDockSide = PreviewDockSide.ToString();
            GrepSettings.Instance.Set(GrepSettings.Key.PreviewAutoPosition, PreviewAutoPosition);
        }

        [ObservableProperty]
        private bool isPreviewDocked = false;

        [ObservableProperty]
        private bool previewAutoPosition = true;

        [ObservableProperty]
        private Dock previewDockSide = Dock.Right;
    }
}
