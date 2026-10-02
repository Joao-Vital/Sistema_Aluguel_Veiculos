using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.API.DTOs
{
    public class CategoriaVeiculoCreateDto
    {
        [Required(ErrorMessage = "O nome da categoria é obrigatório.")]
        [StringLength(50, MinimumLength = 2)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descricao { get; set; }

        [Range(0.01, 100000, ErrorMessage = "O valor da diária base deve ser maior que zero.")]
        public decimal ValorDiariaBase { get; set; }
    }

    public class CategoriaVeiculoReadDto
    {
        public int CategoriaVeiculoId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal ValorDiariaBase { get; set; }
        public int QuantidadeVeiculos { get; set; }
    }
}
