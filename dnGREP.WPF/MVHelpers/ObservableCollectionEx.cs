using System;
using System.Collections.ObjectModel;

namespace dnGREP.WPF
{
    public class ObservableCollectionEx<T> : ObservableCollection<T>
    {
        public event EventHandler? AfterCollectionChanged;

        public void RaiseAfterCollectionChanged()
        {
            AfterCollectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
