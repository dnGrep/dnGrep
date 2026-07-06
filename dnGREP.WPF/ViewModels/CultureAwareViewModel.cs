using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Localization;

namespace dnGREP.WPF
{
    public partial class CultureAwareViewModel : ObservableObject
    {
        public CultureAwareViewModel()
        {
            WeakEventManager<TranslationSource, System.EventArgs>.AddHandler(
                TranslationSource.Instance,
                nameof(TranslationSource.Instance.CurrentCultureChanged),
                OnCurrentCultureChanged);

            CultureFlowDirection = GetFlowDirection();
        }

        private void OnCurrentCultureChanged(object? sender, System.EventArgs e)
        {
            CultureFlowDirection = GetFlowDirection();
        }

        private static FlowDirection GetFlowDirection() =>
            TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;

        [ObservableProperty]
        private FlowDirection cultureFlowDirection = FlowDirection.LeftToRight;
    }
}
