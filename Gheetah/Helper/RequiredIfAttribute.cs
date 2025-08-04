using System.ComponentModel.DataAnnotations;

namespace Gheetah.Helper
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private string Condition { get; }
    
        public RequiredIfAttribute(string condition)
        {
            Condition = condition;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var conditionValue = type.GetProperty(Condition.Split(' ')[0])?.GetValue(instance);
        
            if (Condition.Contains("!= null") && conditionValue != null)
            {
                return value == null 
                    ? new ValidationResult(ErrorMessage) 
                    : ValidationResult.Success;
            }
            return ValidationResult.Success;
        }
    }
}
