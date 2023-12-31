using System.Globalization;
using System.Windows.Controls;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    internal class RequiredValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string text && string.IsNullOrWhiteSpace(text))
            {
                return new ValidationResult(false, Resources.Validation_ValueIsRequired);
            }

            return ValidationResult.ValidResult;
        }
    }
}
