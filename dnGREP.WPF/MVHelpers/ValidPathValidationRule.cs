using System.Globalization;
using System.IO;
using System.Windows.Controls;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    internal class ValidPathValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string path && (string.IsNullOrWhiteSpace(path) || !File.Exists(path)))
            {
                return new ValidationResult(false, Resources.Validation_PathMustBeValid);
            }

            return ValidationResult.ValidResult;
        }
    }
}
