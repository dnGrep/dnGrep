using System.Globalization;
using System.Windows.Controls;
using dnGREP.Localization;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    public class ValueRangeRule : ValidationRule
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public ValueRangeRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string text)
            {
                var style = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

                if (!double.TryParse(text, style, CultureInfo.CurrentCulture, out double num))
                {
                    return new ValidationResult(false, Resources.Validation_stringToDouble_IllegalCharacters);
                }

                if ((num < Min) || (num > Max) || string.IsNullOrEmpty(text))
                {
                    return new ValidationResult(false,
                        TranslationSource.Format(Resources.Validation_stringToDouble_PleaseEnterAValueInTheRange01, Min, Max));
                }
            }
            else
            {
                return new ValidationResult(false, Resources.Validation_stringToDouble_InvalidInputType);
            }
            return ValidationResult.ValidResult;
        }
    }
}