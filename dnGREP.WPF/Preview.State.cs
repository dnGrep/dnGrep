using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.ComponentModel;

namespace dnGREP.WPF
{
    public class PreviewState : INotifyPropertyChanged
    {
        private Visibility isLargeOrBinary;
        public Visibility IsLargeOrBinary { 
            get { return isLargeOrBinary;}
            set { isLargeOrBinary = value; RaisePropertyChanged("IsLargeOrBinary"); }
        }

        private string currentSyntax;
        public string CurrentSyntax
        {
            get { return currentSyntax; }
            set { currentSyntax = value; RaisePropertyChanged("CurrentSyntax"); }
        }

        public List<string> Highlighters { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
