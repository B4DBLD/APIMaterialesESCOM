using System.ComponentModel.DataAnnotations;

namespace APIMaterialesESCOM.Validacion
{
    // Atributo de validación personalizado para verificar que los correos electrónicos
    // pertenezcan a los dominios institucionales del IPN
    public class ValidacionAttribute : ValidationAttribute
    {
        // Valida si el correo electrónico tiene un dominio permitido
        // Parámetros:
        //   value: El valor del correo electrónico a validar
        //   validationContext: Contexto de validación proporcionado por el framework
        // Retorna:
        //   ValidationResult.Success si el correo es válido,
        //   o un mensaje de error si no cumple con los requisitos
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            // Verificar que el valor no sea nulo
            if(value == null)
            {
                return new ValidationResult("El correo electrónico es requerido");
            }

            // Convertir a minúsculas para comparación insensible a mayúsculas
            string email = value.ToString().ToLower();

            // Verificar que el dominio sea uno de los permitidos
            if(email.EndsWith("@alumno.ipn.mx") || email.EndsWith("@ipn.mx"))
            {
                return ValidationResult.Success;
            }

            // Devolver error si el dominio no está en la lista de permitidos
            return new ValidationResult("Solo se permiten correos con dominios @alumno.ipn.mx o @ipn.mx");
        }
    }
}