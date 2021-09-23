using System.Windows;
using dnGREP.Localization;

namespace dnGREP.TestLocalizedStrings
{
    public class CultureAwareViewModel : ViewModelBase
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

        private FlowDirection flowDirection = FlowDirection.LeftToRight;

        public FlowDirection CultureFlowDirection
        {
            get { return flowDirection; }
            set
            {
                if (flowDirection == value)
                    return;

                flowDirection = value;
                OnPropertyChanged(nameof(CultureFlowDirection));
            }
        }

    }
}
