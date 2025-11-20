using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class StringMapViewModel : CultureAwareViewModel
    {
        public event EventHandler? RequestClose;
        private bool itemAdded = false;
        private bool itemDeleted = false;

        public StringMapViewModel()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            StringMap stringMap = GrepSettings.Instance.GetSubstitutionStrings();
            foreach (var item in stringMap.Map)
            {
                MapItems.Add(new StringSubstitutionViewModel(this,
                    item.Key, item.Value));

                MapKeys.Add(item.Key);
            }
        }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        public ObservableCollection<StringSubstitutionViewModel> MapItems { get; } = [];

        public HashSet<string> MapKeys { get; } = [];

        private RelayCommand? saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(
            p => SaveMap(),
            q => itemAdded || itemDeleted);

        private void SaveMap()
        {
            StringMap stringMap = new();

            foreach (var item in MapItems)
            {
                stringMap.Map.Add(item.MapKey, item.MapValue);
            }
            GrepSettings.Instance.SaveSubstitutionStrings(stringMap);

            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        internal void DeleteMapping(StringSubstitutionViewModel item)
        {
            MapItems.Remove(item);
            MapKeys.Remove(item.MapKey);
            itemDeleted = true;
        }

        // properties for Add Mapping:
        [ObservableProperty]
        private string mapKey = string.Empty;

        private bool fromOnMapKeyChanged = false;
        partial void OnMapKeyChanged(string value)
        {
            fromOnMapKeyChanged = true;

            bool duplicate = MapItems.Any(r => r.MapKeyName.Equals(value, StringComparison.Ordinal));

            MapKeyName = StringMap.Describe(value);
            if (!fromOnMapKeyCodePointChanged)
            {
                MapKeyCodePoint = StringToCodePoints(value);
            }

            fromOnMapKeyChanged = false;
        }

        [ObservableProperty]
        private string? mapKeyName;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MapKey))]
        private string mapKeyCodePoint = string.Empty;

        private bool fromOnMapKeyCodePointChanged;
        partial void OnMapKeyCodePointChanged(string value)
        {
            if (!fromOnMapKeyChanged)
            {
                fromOnMapKeyCodePointChanged = true;
                MapKey = CodePointsToString(value);
                fromOnMapKeyCodePointChanged = false;
            }
        }

        [ObservableProperty]
        private string mapValue = string.Empty;

        private bool fromOnMapValueChanged = false;
        partial void OnMapValueChanged(string value)
        {
            fromOnMapValueChanged = true;

            MapValueName = StringMap.Describe(value);
            if (!fromOnMapValueCodePointChanged)
            {
                MapValueCodePoint = StringToCodePoints(value);
            }

            fromOnMapValueChanged = false;
        }

        [ObservableProperty]
        private string? mapValueName;

        [ObservableProperty]
        private string mapValueCodePoint = string.Empty;

        private bool fromOnMapValueCodePointChanged;
        partial void OnMapValueCodePointChanged(string value)
        {
            if (!fromOnMapValueChanged)
            {
                fromOnMapValueCodePointChanged = true;
                MapValue = CodePointsToString(value);
                fromOnMapValueCodePointChanged = false;
            }
        }

        private RelayCommand? addMapCommand;
        public RelayCommand AddMapCommand => addMapCommand ??= new RelayCommand(
            p => AddMapping(),
            q => !string.IsNullOrEmpty(MapKey) &&
                 !MapKeys.Contains(MapKey) &&
                 !string.IsNullOrEmpty(MapValue));

        private RelayCommand? clearFieldsCommand;
        public RelayCommand ClearFieldsCommand => clearFieldsCommand ??= new RelayCommand(
            p => ClearFields(),
            q => !string.IsNullOrEmpty(MapKey) || !string.IsNullOrEmpty(MapValue) ||
                !string.IsNullOrEmpty(MapKeyCodePoint) || string.IsNullOrEmpty(MapValueCodePoint));

        private void AddMapping()
        {
            MapItems.Add(new StringSubstitutionViewModel(this,
                MapKey, MapValue));
            MapKeys.Add(MapKey);

            itemAdded = true;
        }

        private void ClearFields()
        {
            MapKey = string.Empty;
            MapKeyName = null;
            MapKeyCodePoint = string.Empty;
            MapValue = string.Empty;
            MapValueName = null;
            MapValueCodePoint = string.Empty;
        }

        private static string CodePointsToString(string codePoints)
        {
            Match m = CodePointRegex().Match(codePoints);
            if (m != null && m.Success)
            {
                StringBuilder sb = new();
                var cp1 = m.Groups[1].Value;
                if (int.TryParse(cp1[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int num))
                {
                    sb.Append(char.ConvertFromUtf32(num));
                }
                foreach (Capture capture in m.Groups[2].Captures)
                {
                    var cp2 = capture.Value;
                    if (int.TryParse(cp2[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int num2))
                    {
                        sb.Append(char.ConvertFromUtf32(num2));
                    }
                }
                return sb.ToString();
            }
            return string.Empty;
        }

        private static string StringToCodePoints(string value)
        {
            string codePoints = string.Empty;
            foreach (Rune r in value.EnumerateRunes())
            {
                codePoints += $"U+{r.Value:X4} ";
            }
            return codePoints;
        }

        [GeneratedRegex("(?i)^(u\\+[0-9a-fA-F]{4,5})(?:\\s(u\\+[0-9a-fA-F]{4,5}))*$")]
        private static partial Regex CodePointRegex();
    }

    public partial class StringSubstitutionViewModel : CultureAwareViewModel
    {
        public StringSubstitutionViewModel(StringMapViewModel parent,
            string key, string value)
        {
            this.parent = parent;

            MapKey = key;
            MapValue = value;

            string codePoints = string.Empty;
            foreach (Rune r in MapKey.EnumerateRunes())
            {
                codePoints += $"U+{r.Value:X4} ";
            }
            MapKeyCodePoint = codePoints;

            codePoints = string.Empty;
            foreach (Rune r in MapValue.EnumerateRunes())
            {
                codePoints += $"U+{r.Value:X4} ";
            }
            MapValueCodePoint = codePoints;

            MapKeyName = StringMap.Describe(MapKey);
            MapValueName = StringMap.Describe(MapValue);
        }

        private readonly StringMapViewModel parent;

        [ObservableProperty]
        private string mapKey;

        [ObservableProperty]
        private string mapKeyCodePoint;

        [ObservableProperty]
        private string mapKeyName;

        [ObservableProperty]
        private string mapValue;

        [ObservableProperty]
        private string mapValueName;

        [ObservableProperty]
        private string mapValueCodePoint;

        [ObservableProperty]
        private string proposedMapValue = string.Empty;

        private RelayCommand? deleteCommand;
        public RelayCommand DeleteCommand => deleteCommand ??= new RelayCommand(
            p => parent.DeleteMapping(this));
    }
}
