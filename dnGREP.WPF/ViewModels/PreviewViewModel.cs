using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;

namespace dnGREP.WPF
{
    public class PreviewViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private bool isVisible;
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (value == isVisible)
                    return;

                isVisible = value;

                base.OnPropertyChanged(() => IsVisible);
            }
        }

        private Visibility isLargeOrBinary;
        public Visibility IsLargeOrBinary
        {
            get { return isLargeOrBinary; }
            set
            {
                if (value == isLargeOrBinary)
                    return;

                isLargeOrBinary = value;

                base.OnPropertyChanged(() => IsLargeOrBinary);
            }
        }

        private string currentSyntax;
        public string CurrentSyntax
        {
            get { return currentSyntax; }
            set
            {
                if (value == currentSyntax)
                    return;

                currentSyntax = value;

                base.OnPropertyChanged(() => CurrentSyntax);
            }
        }

        public List<string> Highlighters { get; set; }
    }
}
