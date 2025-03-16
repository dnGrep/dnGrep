using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class FormattedGrepMatch : CultureAwareViewModel
    {
        public FormattedGrepMatch(GrepMatch match)
        {
            Match = match;
            ReplaceMatch = Match.ReplaceMatch;

            Background = Match.ReplaceMatch ? Brushes.PaleGreen : Brushes.Bisque;
        }

        public GrepMatch Match { get; }

        public override string ToString()
        {
            return Match.ToString() + $" replace={ReplaceMatch}";
        }

        [ObservableProperty]
        private bool replaceMatch = false;

        partial void OnReplaceMatchChanging(bool value)
        {
            Match.ReplaceMatch = value;
            Background = Match.ReplaceMatch ? Brushes.PaleGreen : Brushes.Bisque;
        }

        [ObservableProperty]
        private Brush background = Brushes.Bisque;

        [ObservableProperty]
        private double fontSize = 12;

        [ObservableProperty]
        private FontWeight fontWeight = FontWeights.Normal;
    }

}
