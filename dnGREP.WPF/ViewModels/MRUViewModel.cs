using System;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public class MRUViewModel : CultureAwareViewModel, IEquatable<MRUViewModel>
    {
        public MRUViewModel(MRUType valueType, string value)
        {
            ValueType = valueType;
            StringValue = value;
        }

        public MRUViewModel(MRUType valueType, string value, bool isPinned)
        {
            ValueType = valueType;
            StringValue = value;
            IsPinned = isPinned;
        }

        public MostRecentlyUsed AsMostRecentlyUsed()
        {
            return new MostRecentlyUsed(StringValue, IsPinned);
        }

        public MRUType ValueType { get; private set; }


        private string stringValue = string.Empty;
        public string StringValue
        {
            get { return stringValue; }
            set
            {
                if (stringValue == value)
                {
                    return;
                }

                stringValue = value;
                OnPropertyChanged(nameof(StringValue));
            }
        }


        private bool isPinned = false;
        public bool IsPinned
        {
            get { return isPinned; }
            set
            {
                if (isPinned == value)
                {
                    return;
                }

                isPinned = value;
                OnPropertyChanged(nameof(IsPinned));
                MainViewModel.MainViewMessenger.NotifyColleagues("IsPinnedChanged", this);
            }
        }

        public string FontFamily { get; set; } = SystemSymbols.FontFamily;

        public string DeleteCharacter { get; set; } = SystemSymbols.DeleteCharacter;

        public string PinCharacter { get; set; } = SystemSymbols.PinCharacter;

        public string UnpinCharacter { get; set; } = SystemSymbols.UnpinCharacter;

        public override int GetHashCode()
        {
            return StringValue.GetHashCode(StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as MRUViewModel);
        }

        public bool Equals(MRUViewModel? other)
        {
            if (other == null)
            {
                return false;
            }
            return string.Equals(StringValue, other.StringValue, StringComparison.OrdinalIgnoreCase);
        }
    }

}