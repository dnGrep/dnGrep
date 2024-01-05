using System.Globalization;
using System.Windows.Controls;
using dnGREP.Localization;
using dnGREP.Localization.Properties;

namespace dnGREP.WPF
{
    public class ValueRangeRuleLong : ValidationRule
    {
        public long Min { get; set; } = 0;
        public long Max { get; set; } = long.MaxValue;

        public ValueRangeRuleLong()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value is string text)
            {
                var style = NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign;

                if (!long.TryParse(text, style, CultureInfo.CurrentCulture, out long num))
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