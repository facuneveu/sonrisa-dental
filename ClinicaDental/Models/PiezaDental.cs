using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    // Una fila del odontograma: estado de una pieza (numeración FDI) de un paciente.
    public class PiezaDental : AuditableEntity
    {
        // Una boca adulta tiene 32 piezas permanentes: 4 cuadrantes de 8.
        public const int TotalPermanentes = 32;

        // Numeración FDI: el primer dígito es el cuadrante (1 a 4) y el segundo la posición desde el centro (1 a 8).
        // Los números válidos son 11-18, 21-28, 31-38 y 41-48: un rango 11..48 aceptaría los que no existen (19, 20, 29, 30...).
        public static bool EsNumeroFdiValido(int numero) => numero / 10 is >= 1 and <= 4 && numero % 10 is >= 1 and <= 8;

        public int Id { get; set; }

        public int PacienteId { get; set; }
        [BindNever, ValidateNever] public Paciente? Paciente { get; set; }

        [PiezaFdi]
        [Display(Name = "Pieza")]
        public int NumeroPieza { get; set; }

        public EstadoPieza Estado { get; set; } = EstadoPieza.Sana;

        [StringLength(300, ErrorMessage = "Las observaciones no pueden superar los {1} caracteres.")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }
    }

    // Valida que el número sea una de las 32 piezas permanentes en notación FDI.
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PiezaFdiAttribute : ValidationAttribute
    {
        public PiezaFdiAttribute()
            : base("La pieza debe ser una de las 32 del odontograma (notación FDI: 11 a 18, 21 a 28, 31 a 38 y 41 a 48).") { }

        public override bool IsValid(object? value) => value is int numero && PiezaDental.EsNumeroFdiValido(numero);
    }
}
