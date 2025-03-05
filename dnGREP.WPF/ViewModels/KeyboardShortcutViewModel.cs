using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using dnGREP.Common;
using dnGREP.Localization;

namespace dnGREP.WPF
{
    public partial class KeyboardShortcutViewModel : CultureAwareViewModel
    {
        public event EventHandler? RequestClose;

        public KeyboardShortcutViewModel()
        {
            ApplicationFontFamily = GrepSettings.Instance.Get<string>(GrepSettings.Key.ApplicationFontFamily);
            DialogFontSize = GrepSettings.Instance.Get<double>(GrepSettings.Key.DialogFontSize);

            var commands = KeyBindingManager.GetCategoryCommands(KeyCategory.Main);
            foreach (var cmd in commands)
            {
                Commands.Add(new CommandGestureViewModel(this, KeyCategory.Main,
                    cmd.CommandName, cmd.Label, cmd.LabelKey, cmd.DefaultKeyGesture, cmd.KeyGesture));
            }

            commands = KeyBindingManager.GetCategoryCommands(KeyCategory.Replace);
            foreach (var cmd in commands)
            {
                Commands.Add(new CommandGestureViewModel(this, KeyCategory.Replace,
                    cmd.CommandName, cmd.Label, cmd.LabelKey, cmd.DefaultKeyGesture, cmd.KeyGesture));
            }

            commands = KeyBindingManager.GetCategoryCommands(KeyCategory.Bookmark);
            foreach (var cmd in commands)
            {
                Commands.Add(new CommandGestureViewModel(this, KeyCategory.Bookmark,
                    cmd.CommandName, cmd.Label, cmd.LabelKey, cmd.DefaultKeyGesture, cmd.KeyGesture));
            }

            commands = KeyBindingManager.GetCategoryCommands(KeyCategory.Script);
            foreach (var cmd in commands)
            {
                Commands.Add(new CommandGestureViewModel(this, KeyCategory.Script,
                    cmd.CommandName, cmd.Label, cmd.LabelKey, cmd.DefaultKeyGesture, cmd.KeyGesture));
            }

            SelectedCommand = Commands.First();
        }

        [ObservableProperty]
        private string applicationFontFamily = SystemFonts.MessageFontFamily.Source;

        [ObservableProperty]
        private double dialogFontSize;

        public ObservableCollection<CommandGestureViewModel> Commands { get; } = [];

        [ObservableProperty]
        private CommandGestureViewModel selectedCommand;


        private RelayCommand? resetCommand;
        public RelayCommand ResetCommand => resetCommand ??= new RelayCommand(
            p => ResetToDefaults(),
            q => Commands.Any(r => r.KeyGesture != r.DefaultKeyGesture));

        private RelayCommand? saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(
            p => SaveGestures(),
            q => Commands.Any(r => r.IsDirty));

        private void ResetToDefaults()
        {
            foreach (var cmd in Commands.Where(r => r.KeyGesture != r.DefaultKeyGesture))
            {
                cmd.KeyGesture = cmd.DefaultKeyGesture;
                cmd.ProposedKeyGesture = cmd.KeyGesture;
            }
        }

        private void SaveGestures()
        {
            HashSet<KeyCategory> categories = [];
            foreach (var cmd in Commands.Where(r => r.IsDirty))
            {
                var kbi = KeyBindingManager.GetCategoryCommands(cmd.Category)
                    .FirstOrDefault(r => r.CommandName == cmd.CommandName);

                if (kbi != null)
                {
                    kbi.KeyGesture = cmd.KeyGesture;
                    categories.Add(cmd.Category);
                }
            }
            KeyBindingManager.SaveBindings();

            foreach (var category in categories)
                App.Messenger.NotifyColleagues("KeyGestureChanged", category);

            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }

    public partial class CommandGestureViewModel : CultureAwareViewModel
    {
        public CommandGestureViewModel(KeyboardShortcutViewModel parent, KeyCategory category,
            string commandName, string label, string labelKey,
            string defaultKeyGesture, string keyGesture)
        {
            this.parent = parent;
            originalKeyGesture = keyGesture;
            Category = category;
            CategoryName = KeyGestureLocalizer.ToString(category);
            CommandName = commandName;
            CommandShortName = commandName.EndsWith("Command", StringComparison.Ordinal) ? commandName[..^7] : commandName;
            DefaultKeyGesture = defaultKeyGesture;
            KeyGesture = keyGesture;

            if (!string.IsNullOrEmpty(keyGesture))
            {
                ProposedKeyGesture = keyGesture;
            }

            if (!string.IsNullOrEmpty(label))
            {
                Label = label;
            }
            else if (!string.IsNullOrEmpty(labelKey))
            {
                Label = TranslationSource.Instance[labelKey]
                    .Replace("_", "", StringComparison.Ordinal)
                    .Replace("…", "", StringComparison.Ordinal);
            }
            else
            {
                Label = string.Empty;
            }
        }

        private bool allowedChange = true;

        private readonly KeyboardShortcutViewModel parent;

        private readonly string originalKeyGesture;

        public bool IsDirty => KeyGesture != originalKeyGesture;

        public string CommandName { get; }

        public KeyCategory Category { get; }


        [ObservableProperty]
        private string categoryName;

        [ObservableProperty]
        private string commandShortName;

        [ObservableProperty]
        private string label;

        [ObservableProperty]
        private string defaultKeyGesture;

        [ObservableProperty]
        private string keyGesture;

        [ObservableProperty]
        private string localizedKeyGesture = string.Empty;

        partial void OnKeyGestureChanged(string value)
        {
            LocalizedKeyGesture = KeyGestureLocalizer.LocalizeKeyGestureText(value);
        }

        [ObservableProperty]
        private string proposedKeyGesture = string.Empty;

        [ObservableProperty]
        private string localizedProposedKeyGesture = string.Empty;

        partial void OnProposedKeyGestureChanged(string value)
        {
            LocalizedProposedKeyGesture = KeyGestureLocalizer.LocalizeKeyGestureText(value);

            CheckForDuplicate(value);
        }

        [ObservableProperty]
        private string duplicateCommandName = string.Empty;

        private void CheckForDuplicate(string newGesture)
        {
            DuplicateCommandName = string.Empty;
            allowedChange = true;

            if (!string.IsNullOrEmpty(newGesture))
            {
                if (newGesture == "F1")
                {
                    DuplicateCommandName = "Help";
                    allowedChange = false;
                    return;
                }

                CommandGestureViewModel? other = parent.Commands
                    .FirstOrDefault(r => r.Category == Category &&
                        !string.IsNullOrEmpty(r.KeyGesture) &&
                        r.KeyGesture.Equals(newGesture, StringComparison.Ordinal) &&
                        r.CommandName != CommandName);

                if (other != null)
                {
                    DuplicateCommandName = other.CommandName;
                }
            }
        }

        private RelayCommand? assignGestureCommand;
        public RelayCommand AssignGestureCommand => assignGestureCommand ??= new RelayCommand(
            p => AssignKeyGesture(),
            q => allowedChange && !string.IsNullOrEmpty(ProposedKeyGesture) && ProposedKeyGesture != KeyGesture);

        private RelayCommand? removeGestureCommand;
        public RelayCommand RemoveGestureCommand => removeGestureCommand ??= new RelayCommand(
            p => RemoveGesture(),
            q => !string.IsNullOrEmpty(KeyGesture));


        private void AssignKeyGesture()
        {
            KeyGesture = ProposedKeyGesture;

            if (!string.IsNullOrEmpty(DuplicateCommandName))
            {
                CommandGestureViewModel? other = parent.Commands
                    .FirstOrDefault(r => r.Category == Category &&
                        r.CommandName == DuplicateCommandName);
                if (other != null)
                {
                    other.KeyGesture = string.Empty;
                    other.ProposedKeyGesture = string.Empty;
                }

                DuplicateCommandName = string.Empty;
            }
        }

        private void RemoveGesture()
        {
            KeyGesture = string.Empty;
            ProposedKeyGesture = string.Empty;
        }
    }
}
