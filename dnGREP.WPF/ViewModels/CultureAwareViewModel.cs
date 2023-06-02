using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Localization;

namespace dnGREP.WPF
{
    public partial class CultureAwareViewModel : ObservableObject
    {
        public CultureAwareViewModel()
        {
            TranslationSource.Instance.CurrentCultureChanged += (s, e) =>
            {
                CultureFlowDirection = TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft ?
                    FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            };

            CultureFlowDirection = TranslationSource.Instance.CurrentCulture.TextInfo.IsRightToLeft ?
                    FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        }

        [ObservableProperty]
        private FlowDirection cultureFlowDirection = FlowDirection.LeftToRight;

    }
}
