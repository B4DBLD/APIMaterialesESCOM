using System.ComponentModel.DataAnnotations;
namespace APIMaterialesESCOM.Validacion
{
        public class ValidacionAttribute : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
            if (value == null)
            {
            return new ValidationResult("El correo electrónico es requerido");
            }

            string email = value.ToString().ToLower();

            if (email.EndsWith("@alumno.ipn.mx") || email.EndsWith("@ipn.mx"))
            {
            return ValidationResult.Success;
            }

            return new ValidationResult("Solo se permiten correos con dominios @alumno.ipn.mx o @ipn.mx");
            }
        }
}
