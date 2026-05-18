using System.ComponentModel.DataAnnotations;
using System.Reflection;


namespace scsValidationLib.Areas.Validation
{
    public static class ValidationAttributes
    {

        public class NotZeroValidation : ValidationAttribute
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                double doubleValue = Convert.ToDouble(value);
                if (Math.Abs(doubleValue) > double.Epsilon)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult("The value cannot be zero.", new string[] { validationContext.MemberName });
                }
            }
        }

        public class RequireIfAnotherPropertyIsNotZero : ValidationAttribute
        {
            private readonly string[] _dependentProperties;

            public RequireIfAnotherPropertyIsNotZero(params string[] dependentProperties)
            {
                _dependentProperties = dependentProperties;
            }

            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                var memberDisplayName = validationContext.DisplayName ?? validationContext.MemberName;

                foreach (string dependentProperty in _dependentProperties)
                {
                    var dependentPropertyValue = validationContext.ObjectInstance.GetType().GetProperty(dependentProperty)?.GetValue(validationContext.ObjectInstance);

                    var dependentPropertyDisplayName = validationContext.ObjectType.GetProperty(dependentProperty)?.GetCustomAttributes(typeof(DisplayAttribute), true).Cast<DisplayAttribute>().SingleOrDefault()?.GetName() ?? dependentProperty;

                    if (dependentPropertyValue is null)
                    {
                        continue;
                    }

                    if (!double.TryParse(dependentPropertyValue.ToString(), out double dependentPropertyDoubleValue))
                    {
                        return new ValidationResult($"The dependent property {dependentProperty} must be a double.", new string[] { dependentProperty });
                    }

                    if (dependentPropertyDoubleValue != 0 && (value == null || (decimal)value == 0))
                    {
                        return new ValidationResult($"If {dependentPropertyDisplayName} is not zero, {memberDisplayName} must have a value.", new string[] { validationContext.MemberName });
                    }
                }

                return ValidationResult.Success;
            }


        }

        public class NotBeforeYear1900Validation : ValidationAttribute
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                DateTime date = (DateTime)value;
                DateTime year1900 = new DateTime(1990, 1, 1);

                if (date >= year1900)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult($"{validationContext.DisplayName} cannot be before the year 1900.", new string[] { validationContext.MemberName });
                }
            }
        }

        public class NotFutureDateValidation : ValidationAttribute
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                DateTime date = (DateTime)value;
                if (date <= DateTime.Now)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult($"{validationContext.DisplayName} cannot be in the future.", new string[] { validationContext.MemberName });
                }
            }
        }

        public class NotNegative : ValidationAttribute
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                if (value == null)
                {
                    return new ValidationResult($"{validationContext.DisplayName} cannot be null.", new string[] { validationContext.MemberName });
                }

                if ((value is decimal decVal && decVal >= 0m) || (value is int intVal && intVal >= 0))
                {
                    return ValidationResult.Success;
                }

                if (!(value is decimal || value is int))
                {
                    return new ValidationResult("Invalid type.", new string[] { validationContext.MemberName });
                }

                return new ValidationResult($"{validationContext.DisplayName} cannot be negative.", new string[] { validationContext.MemberName });
            }
        }


        public class NotNegativeButCanBeNull : ValidationAttribute //TODO: Change to NotNegative ?
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                if (value == null)
                {
                    return ValidationResult.Success;
                }
                decimal val = (decimal)value;
                if (value != null && val >= 0)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    return new ValidationResult($"{validationContext.DisplayName} cannot be negative.", new string[] { validationContext.MemberName });
                }
            }
        }

        public class IRDValidation : ValidationAttribute
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                if (validationContext != null)
                {
                    if (value != null)
                    {
                        string pattern = @"^(\d{3}-\d{3}-\d{3})";
                        string valueString = value.ToString();

                        bool irdFormatMatch = System.Text.RegularExpressions.Regex.IsMatch(valueString, pattern);

                        if (!irdFormatMatch)
                        {
                            return new ValidationResult("Invalid IRD number. Must be: xxx-xxx-xx format.", new string[] { validationContext.MemberName });
                        }
                    }
                    return ValidationResult.Success;
                }
                throw new ArgumentNullException(nameof(validationContext));
            }
        }
        public class PercentValidation : ValidationAttribute
        {
            protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
            {
                if (validationContext != null)
                {
                    if (value != null)
                    {
                        int v = 0;
                        try
                        {
                            v = int.Parse(value.ToString());
                        }
                        catch
                        {
                            return new ValidationResult("Invalid Percentage Value.", new string[] { validationContext.MemberName });
                        }
                        if (v < 0 | v > 100)
                        {
                            return new ValidationResult("Invalid Percentage Value.", new string[] { validationContext.MemberName });
                        }
                    }
                    return ValidationResult.Success;
                }
                throw new ArgumentNullException(nameof(validationContext));
            }
        }

        public enum CompareOperator
        {
            GreaterThan,
            GreaterThanOrEqualTo,
            LessThan,
            LessThanOrEqualTo,
            EqualTo,
            NotEqualTo
        }

        public class CompareDateValidation : ValidationAttribute
        {
            private readonly string _dependentPropertyName;
            private readonly CompareOperator _operatorType;

            public CompareDateValidation(string dependentPropertyName, CompareOperator operatorType)
            {
                _dependentPropertyName = dependentPropertyName;
                _operatorType = operatorType;
            }

            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                DateTime date = (DateTime)value;
                PropertyInfo dependentProperty = validationContext.ObjectType.GetProperty(_dependentPropertyName);

                if (dependentProperty == null)
                {
                    return new ValidationResult($"Property '{_dependentPropertyName}' not found.");
                }

                var dependentPropertyDisplayName = validationContext.ObjectType.GetProperty(_dependentPropertyName)?.GetCustomAttributes(typeof(DisplayAttribute), true)
                    .Cast<DisplayAttribute>()
                    .SingleOrDefault()?.GetName();

                DateTime dependentDate = (DateTime)dependentProperty.GetValue(validationContext.ObjectInstance);

                bool isValid;

                switch (_operatorType)
                {
                    case CompareOperator.GreaterThan:
                        isValid = date > dependentDate;
                        break;
                    case CompareOperator.GreaterThanOrEqualTo:
                        isValid = date >= dependentDate;
                        break;
                    case CompareOperator.LessThan:
                        isValid = date < dependentDate;
                        break;
                    case CompareOperator.LessThanOrEqualTo:
                        isValid = date <= dependentDate;
                        break;
                    case CompareOperator.EqualTo:
                        isValid = date == dependentDate;
                        break;
                    case CompareOperator.NotEqualTo:
                        isValid = date != dependentDate;
                        break;
                    default:
                        isValid = false;
                        break;
                }

                if (isValid)
                {
                    return ValidationResult.Success;
                }
                else
                {
                    string operatorTypeString = ValidationHelper.GetFormattedEnumString(_operatorType);
                    return new ValidationResult($"{validationContext.DisplayName} must be {operatorTypeString}: {dependentPropertyDisplayName}", new string[] { validationContext.MemberName });
                }
            }
        }

    }
}
