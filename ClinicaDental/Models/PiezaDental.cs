using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ClinicaDental.Models
{
    public class PiezaDental : AuditableEntity
    {
        public const int TotalPermanentes = 32;

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

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PiezaFdiAttribute : ValidationAttribute
    {
        public PiezaFdiAttribute()
            : base("La pieza debe ser una de las 32 del odontograma (notación FDI: 11 a 18, 21 a 28, 31 a 38 y 41 a 48).") { }

        public override bool IsValid(object? value) => value is int numero && PiezaDental.EsNumeroFdiValido(numero);
    }
}
