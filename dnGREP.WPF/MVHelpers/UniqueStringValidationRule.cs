using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace dnGREP.WPF
{
    // these classes are based on this article:
    // https://learn.microsoft.com/en-us/archive/technet-wiki/31422.wpf-passing-a-data-bound-value-to-a-validation-rule

    public class UniqueStringValidationRule : ValidationRule
    {
        public KeyList? KeyList { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string newKey && KeyList?.Items != null && KeyList.Items.Contains(newKey))
            {
                return new ValidationResult(false, ErrorMessage);
            }
            return ValidationResult.ValidResult;
        }
    }

    public class KeyList : DependencyObject
    {
        public static readonly DependencyProperty ItemsProperty =
             DependencyProperty.Register(
                 "Items",
                 typeof(HashSet<string>),
                 typeof(KeyList),
                 new PropertyMetadata(null));

        public HashSet<string> Items
        {
            get { return (HashSet<string>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data",
                typeof(object), typeof(BindingProxy),
                new PropertyMetadata(null));
    }
}
