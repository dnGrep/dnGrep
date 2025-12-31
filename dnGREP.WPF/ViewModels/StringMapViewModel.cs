using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;

namespace dnGREP.WPF
{
    public partial class StringMapViewModel : CultureAwareViewModel
    {
        public event EventHandler<DataEventArgs<int>>? SetFocus;
        public event EventHandler? RequestClose;

        private readonly List<StringSubstitutionViewModel> _items = [];
        private bool itemAdded = false;
        private bool itemDeleted = false;
        private bool isReordered = false;
        private static bool beenInitialized;

        static StringMapViewModel()
        {
            Initialize();
        }

        public StringMapViewModel()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            StringMap stringMap = GrepSettings.Instance.GetSubstitutionStrings();

            _items.Clear();

            foreach (var item in stringMap.Map)
            {
                _items.Add(new StringSubstitutionViewModel(this,
                    item.Key, item.Value, _items.Count));

                MapKeys.Add(item.Key);
            }
            MapItems = CollectionViewSource.GetDefaultView(_items);

            InitializeInputBindings();
        }

        public static void Initialize()
        {
            if (beenInitialized) return;

            beenInitialized = true;
            KeyBindingManager.RegisterCommand(KeyCategory.StringMap, nameof(MoveToTopCommand), "StringMap_MoveToTop", "Shift+Alt+Up");
            KeyBindingManager.RegisterCommand(KeyCategory.StringMap, nameof(MoveUpCommand), "StringMap_MoveUp", "Alt+Up");
            KeyBindingManager.RegisterCommand(KeyCategory.StringMap, nameof(MoveDownCommand), "StringMap_MoveDown", "Alt+Down");
            KeyBindingManager.RegisterCommand(KeyCategory.StringMap, nameof(MoveToBottomCommand), "StringMap_MoveToBottom", "Shift+Alt+Down");
        }

        private void InitializeInputBindings()
        {
            foreach (KeyBindingInfo kbi in KeyBindingManager.GetCommandGestures(KeyCategory.StringMap))
            {
                PropertyInfo? pi = GetType().GetProperty(kbi.CommandName, BindingFlags.Instance | BindingFlags.Public);
                if (pi != null && pi.GetValue(this) is RelayCommand cmd)
                {
                    InputBindings.Add(KeyBindingManager.CreateKeyBinding(cmd, kbi.KeyGesture));
                }
            }
        }

        public ObservableCollectionEx<InputBinding> InputBindings { get; } = [];

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        [ObservableProperty]
        private StringSubstitutionViewModel? selectedItem = null;

        partial void OnSelectedItemChanged(StringSubstitutionViewModel? value)
        {
        }

        public ICollectionView MapItems { get; private set; }

        public HashSet<string> MapKeys { get; } = [];

        private RelayCommand? saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(
            p => SaveMap(),
            q => itemAdded || itemDeleted || isReordered);

        private void SaveMap()
        {
            StringMap stringMap = new();

            foreach (var item in _items.OrderBy(i => i.Ordinal))
            {
                stringMap.Map.Add(item.MapKey, item.MapValue);
            }
            GrepSettings.Instance.SaveSubstitutionStrings(stringMap);

            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        internal void DeleteMapping(StringSubstitutionViewModel item)
        {
            _items.Remove(item);
            MapKeys.Remove(item.MapKey);
            itemDeleted = true;
            MapItems.Refresh();
        }

        // properties for Add Mapping:
        [ObservableProperty]
        private string mapKey = string.Empty;

        private bool fromOnMapKeyChanged = false;
        partial void OnMapKeyChanged(string value)
        {
            fromOnMapKeyChanged = true;

            bool duplicate = _items.Any(r => r.MapKeyName.Equals(value, StringComparison.Ordinal));

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

        private RelayCommand? moveToTopCommand;
        public RelayCommand MoveToTopCommand => moveToTopCommand ??= new RelayCommand(
            p => MoveToTop(),
            q => SelectedItem != null && SelectedItem.Ordinal > 0);

        private RelayCommand? moveUpCommand;
        public RelayCommand MoveUpCommand => moveUpCommand ??= new RelayCommand(
            p => MoveUp(),
            q => SelectedItem != null && SelectedItem.Ordinal > 0);

        private RelayCommand? moveDownCommand;
        public RelayCommand MoveDownCommand => moveDownCommand ??= new RelayCommand(
            p => MoveDown(),
            q => SelectedItem != null && SelectedItem.Ordinal < _items.Count - 1);

        private RelayCommand? moveToBottomCommand;
        public RelayCommand MoveToBottomCommand => moveToBottomCommand ??= new RelayCommand(
            p => MoveToBottom(),
            q => SelectedItem != null && SelectedItem.Ordinal < _items.Count - 1);

        private void AddMapping()
        {
            _items.Add(new StringSubstitutionViewModel(this,
                MapKey, MapValue, _items.Count));
            MapKeys.Add(MapKey);
            MapItems.Refresh();

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


        private void UpdateOrder()
        {
            isReordered = true;
            Sort();
            MapItems.Refresh();
            if (SelectedItem != null)
            {
                int idx = _items.IndexOf(SelectedItem);
                SetFocus?.Invoke(this, new DataEventArgs<int>(idx));
            }
        }

        public void Sort()
        {
            _items.Sort((x, y) => x.Ordinal.CompareTo(y.Ordinal));
        }

        private void MoveToTop()
        {
            if (SelectedItem != null)
            {
                int idx = SelectedItem.Ordinal;
                if (idx > 0)
                {
                    SelectedItem.Ordinal = 0;
                    foreach (StringSubstitutionViewModel item in MapItems)
                    {
                        if (item != SelectedItem && item.Ordinal < idx)
                        {
                            item.Ordinal++;
                        }
                    }
                    UpdateOrder();
                }
            }
        }

        private void MoveUp()
        {
            if (SelectedItem != null)
            {
                int idx = SelectedItem.Ordinal;
                if (idx > 0)
                {
                    var prev = _items.Where(b => b.Ordinal == idx - 1).First();
                    SelectedItem.Ordinal = prev.Ordinal;
                    prev.Ordinal = idx;
                    UpdateOrder();
                }
            }
        }

        private static readonly char[] newlines = ['\n', '\r'];

        private void MoveDown()
        {
            if (SelectedItem != null)
            {
                int idx = SelectedItem.Ordinal;
                if (idx < _items.Count - 1)
                {
                    var next = _items.Where(b => b.Ordinal == idx + 1).First();
                    SelectedItem.Ordinal = next.Ordinal;
                    next.Ordinal = idx;
                    UpdateOrder();
                }
            }
        }

        private void MoveToBottom()
        {
            if (SelectedItem != null)
            {
                int idx = SelectedItem.Ordinal;
                if (idx < _items.Count - 1)
                {
                    SelectedItem.Ordinal = _items.Count - 1;
                    foreach (StringSubstitutionViewModel item in MapItems)
                    {
                        if (item != SelectedItem && item.Ordinal > idx)
                        {
                            item.Ordinal--;
                        }
                    }
                    UpdateOrder();
                }
            }
        }

        [GeneratedRegex("(?i)^(u\\+[0-9a-fA-F]{4,5})(?:\\s(u\\+[0-9a-fA-F]{4,5}))*$")]
        private static partial Regex CodePointRegex();
    }

    public partial class StringSubstitutionViewModel : CultureAwareViewModel
    {
        public StringSubstitutionViewModel(StringMapViewModel parent,
            string key, string value, int ordinal)
        {
            this.parent = parent;

            MapKey = key;
            MapValue = value;
            Ordinal = ordinal;

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
        private int ordinal = 0;

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
