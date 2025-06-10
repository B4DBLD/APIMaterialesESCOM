using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace APIMaterialesESCOM.Validacion
{
    public class ValidarBoletaAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return new ValidationResult("La boleta es requerida");
            }

            string boleta = value.ToString().Trim();

            // Patrón para validar formato 20XX63XXXX donde XX son dígitos
            string patron = @"^20(\d{2})63\d{4}$";
            
            var match = Regex.Match(boleta, patron);
            if (!match.Success)
            {
                return new ValidationResult("El formato de la boleta debe ser 20XX63XXXX (ejemplo: 2024631234)");
            }

            // Extraer el año de la boleta
            int anioBoleta = int.Parse("20" + match.Groups[1].Value);
            int anioActual = DateTime.Now.Year;

            // Validar que el año esté dentro del rango permitido (desde 2000 hasta año actual + 1)
            if (anioBoleta < 2000 || anioBoleta > anioActual + 1)
            {
                return new ValidationResult($"El año de la boleta debe estar entre 2000 y {anioActual + 1}");
            }

            return ValidationResult.Success;
        }
    }
}