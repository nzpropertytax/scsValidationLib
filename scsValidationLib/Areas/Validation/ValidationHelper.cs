using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace scsValidationLib.Areas.Validation
{
    public static class ValidationHelper
    {
        public static class ValidationStatus
        {
            public const string Valid = "Valid";
            public const string Invalid = "InValid";
        }

        public class MultipleValidationResult : ValidationResult
        {
            private readonly List<ValidationResult> _validationResults;

            public MultipleValidationResult(string errorMessage, List<ValidationResult> validationResults)
                : base(errorMessage)
            {
                _validationResults = validationResults;
            }

            public IEnumerable<ValidationResult> ValidationResults
            {
                get { return _validationResults; }
            }
        }

        public class ValidationWarningResult : ValidationResult
        {
            public ValidationWarningResult(string warningMessage, IEnumerable<string>? memberNames) : base(warningMessage, memberNames) { }
            public ValidationWarningResult(string warningMessage) : base(warningMessage) { }
        }

        public static List<ValidationResult> ValidateModel<T>(T model, ICollection<ValidationResult>? validationResults = null)
        {
            try
            {
                //if existing validation results are not passed in then create a new list
                validationResults ??= new List<ValidationResult>();

                if (model == null)
                {
                    throw new ArgumentNullException(nameof(model));
                }

                var validationContext = new ValidationContext(model);

                //validationResults is being added to by the TryValidateObject method
                if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
                {
                    foreach (var result in validationResults.ToList())
                    {
                        //if there are multiple validations then add them into the list
                        if (result is MultipleValidationResult multipleValidationResult)
                        {
                            foreach (var innerValidationResult in multipleValidationResult.ValidationResults)
                            {
                                //if the exact same error message already exists for the same field then don't add it.
                                if (!validationResults.Any(x => x.ErrorMessage == innerValidationResult.ErrorMessage))
                                {
                                    validationResults.Add(innerValidationResult);
                                }
                            }
                            //remove the result that contained the inner results
                            validationResults.Remove(result);
                        }
                    }
                    return (List<ValidationResult>)validationResults;
                }
                return (List<ValidationResult>)validationResults;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while validating model '{typeof(T).Name}': {ex.Message}");
            }
        }

        /// <summary>
        /// A generic method to validate a property using a specified validation attribute.
        /// </summary>
        /// <typeparam name="TValidation">The type of the validation attribute to apply (must inherit from ValidationAttribute).</typeparam>
        /// <param name="validationErrors">A list of validation errors to update with any new validation errors.</param>
        /// <param name="value">The value of the property to be validated.</param>
        /// <param name="fieldId">The identifier of the field being validated.</param>
        /// <param name="displayName">An optional display name for the field being validated. Defaults to an empty string.</param>
        /// <returns>A list of validation errors, updated with any new errors resulting from the specified validation.</returns>
        /// <exception cref="ArgumentException">Thrown when the TValidation type does not inherit from ValidationAttribute.</exception>
        ///
        /// Usage example:
        /// validationErrors = ValidateProperty<NotFutureDateValidation>(validationErrors, date, fieldID, displayName);
        /// validationErrors = ValidateProperty<NotBeforeYear1900Validation>(validationErrors, date, fieldID, displayName);

        public static List<ValidationResult> ValidateProperty<TValidation>(List<ValidationResult> validationErrors, object value, string fieldId, string? displayName = "")
        {
            var validator = Activator.CreateInstance<TValidation>() as ValidationAttribute;
            if (validator == null)
            {
                throw new ArgumentException("TValidation must inherit from ValidationAttribute.");
            }

            var context = new ValidationContext(value, null, null) { MemberName = fieldId };
            context.DisplayName = displayName;
            var validationResult = validator.GetValidationResult(value, context);

            if (validationResult != ValidationResult.Success)
            {
                validationErrors.Add(validationResult);
            }

            return validationErrors;
        }

        private static List<ValidationResult> ValidateField<T>(T model, string fieldName, List<ValidationResult>? validationResults = null)
        {
            validationResults ??= new List<ValidationResult>();

            var field = model.GetType().GetProperty(fieldName);
            if (field != null)
            {
                var value = field.GetValue(model);
                var validationAttribute = field.GetCustomAttributes<ValidationAttribute>();
                foreach (var attribute in validationAttribute)
                {
                    // Create a new validation context with the necessary properties
                    var validationContext = new ValidationContext(model)
                    {
                        MemberName = fieldName,
                        DisplayName = field.Name
                    };
                    ValidationResult validationResult = attribute.GetValidationResult(value, validationContext);
                    if (validationResult != ValidationResult.Success)
                    {
                        validationResults.Add(validationResult);
                    }
                }
            }
            return validationResults;
        }

        //Exclusively valitate these fields passed in
        public static List<ValidationResult> ValidateFields<T>(T model, string[] fieldNames, List<ValidationResult>? validationResults = null)
        {
            validationResults ??= new List<ValidationResult>();
            foreach (var fieldName in fieldNames)
            {
                validationResults = ValidateField(model, fieldName, validationResults);
            }
            return validationResults;
        }

        public static string GetFieldDisplayName(string FieldName, Type ModelType)
        {
            var field = ModelType.GetProperty(FieldName);

            if (field != null)
            {
                var displayAttribute = field.GetCustomAttribute(typeof(DisplayAttribute)) as DisplayAttribute;

                //if null return passed in fieldName
                if (displayAttribute == null)
                {
                    return FieldName;
                }

                return displayAttribute.Name;
            }

            return FieldName;
        }


        public static string GetFormattedEnumString(Enum value)
        {
            string enumString = value.ToString();
            StringBuilder formattedString = new StringBuilder();

            for (int i = 0; i < enumString.Length; i++)
            {
                if (i > 0 && char.IsUpper(enumString[i]))
                {
                    formattedString.Append(' ');
                }
                formattedString.Append(char.ToLower(enumString[i]));
            }

            return formattedString.ToString();
        }
        public static string FormatErrorMessage(IList<ValidationResult> validation)
        {
                StringBuilder sb = new StringBuilder();
                foreach (var item in validation)
                {
                    sb.AppendFormat("{0}: {1}, ", item.MemberNames, item.ErrorMessage);
                }
                return sb.ToString().TrimEnd(',', ' ');
        }

    }

}
