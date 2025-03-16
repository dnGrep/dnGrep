﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace dnGREP.Localization
{
    public partial class TranslationSource : INotifyPropertyChanged
    {
        public static TranslationSource Instance { get; } = new TranslationSource();

        private TranslationSource()
        {
            CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag("en");
        }

        // WPF bindings register PropertyChanged event if the object supports it and update themselves when it is raised
        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? CurrentCultureChanging;
        public event EventHandler? CurrentCultureChanged;

        public static Dictionary<string, string> AppCultures => new()
            {
                { "ar", "العربية" },
                { "bg", "Български" },
                { "ca", "català" },
                { "de", "Deutsch" },
                { "en", "English" },
                { "es", "español" },
                { "et", "eesti" },
                { "fr", "français" },
                { "he", "עברית" },
                { "hr", "hrvatski" },
                { "it", "italiano" },
                { "ja", "日本語" },
                { "ko", "한국어" },
                { "nb-NO", "norsk (bokmål)" },
                { "pl", "polski" },
                { "pt", "Português" },
                { "ru", "pусский" },
                { "sk", "slovenčina" },
                { "sr", "српски" },
                { "sv", "svenska" },
                { "th", "ไทย" },
                { "tr", "Türkçe" },
                { "uk", "українська" },
                { "zh-CN", "简体中文" },
                { "zh-Hant", "繁體中文" },
            };

        public void SetCulture(string ietfLanguageTag)
        {
            if (!string.IsNullOrWhiteSpace(ietfLanguageTag) && AppCultures.ContainsKey(ietfLanguageTag))
            {
                ResourceManagerEx.Instance.FileResources = null;
                CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(ietfLanguageTag);
            }
        }

        public string this[string key] => Properties.Resources.ResourceManager.GetString(key, currentCulture) ?? $"#{key}";

        private CultureInfo currentCulture = CultureInfo.InvariantCulture;
        public CultureInfo CurrentCulture
        {
            get { return currentCulture; }
            set
            {
                if (currentCulture != value)
                {
                    CurrentCultureChanging?.Invoke(this, EventArgs.Empty);

                    currentCulture = value;
                    Properties.Resources.Culture = value;
                    Thread.CurrentThread.CurrentUICulture = value;
#if DEBUG
                    Thread.CurrentThread.CurrentCulture = value;
#endif

                    // string.Empty/null indicates that all properties have changed
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

                    CurrentCultureChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool LoadResxFile(string filePath)
        {
            ResxFile resxFile = new();
            resxFile.ReadFile(filePath);
            if (resxFile.IsValid)
            {
                ResourceManagerEx.Instance.FileResources = resxFile;
                CurrentCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(resxFile.IetfLanguageTag);
                return true;
            }
            return false;
        }

        [GeneratedRegex("{\\d+(:\\w+)?}")]
        private static partial Regex PlaceholderRegex();

        public static string Format(string format, params object[] args)
        {
            if (!string.IsNullOrWhiteSpace(format))
            {
#if DEBUG
                var matchCount = PlaceholderRegex().Matches(format).Count;
                if (matchCount != args.Length)
                {
                    return "Missing placeholder {?}: " + format;
                }
#endif
                try
                {
                    return string.Format(CultureInfo.CurrentCulture, format, args);
                }
                catch (FormatException)
                {
                    return "Missing placeholder {?}: " + format;
                }
            }
            else
            {
                return "Missing resource";
            }
        }

        public static int CountPlaceholders(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                return PlaceholderRegex().Matches(format).Count;
            }
            return 0;
        }

        public MessageBoxOptions FlowDirection => CurrentCulture.TextInfo.IsRightToLeft ?
            MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign : MessageBoxOptions.None;
    }

    [MarkupExtensionReturnType(typeof(string))]
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(Key))
            {
                throw new ArgumentException("Key must be set");
            }

            // targetObject is the control that is using the LocExtension
            object? targetObject = (serviceProvider as IProvideValueTarget)?.TargetObject;

            if (targetObject?.GetType().Name == "SharedDp") // is extension used in a control template?
                return targetObject; // required for template re-binding

            Binding binding = new()
            {
                Mode = BindingMode.OneWay,
                Path = new PropertyPath($"[{Key}]"),
                Source = TranslationSource.Instance,
            };

            object localizedValue = binding.ProvideValue(serviceProvider);

            return localizedValue;
        }
    }
}
