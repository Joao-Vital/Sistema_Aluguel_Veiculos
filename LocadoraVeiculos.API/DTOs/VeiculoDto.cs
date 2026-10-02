using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.API.DTOs
{
    public class VeiculoCreateDto
    {
        [Required(ErrorMessage = "O modelo do veículo é obrigatório.")]
        [StringLength(60, MinimumLength = 2)]
        public string Modelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A placa é obrigatória.")]
        [StringLength(8, MinimumLength = 7, ErrorMessage = "A placa deve ter entre 7 e 8 caracteres.")]
        public string Placa { get; set; } = string.Empty;

        [Required]
        [Range(1950, 2100, ErrorMessage = "Ano de fabricação inválido.")]
        public int AnoFabricacao { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "A quilometragem não pode ser negativa.")]
        public int Quilometragem { get; set; }

        [Required(ErrorMessage = "O fabricante é obrigatório.")]
        public int FabricanteId { get; set; }

        [Required(ErrorMessage = "A categoria do veículo é obrigatória.")]
        public int CategoriaVeiculoId { get; set; }
    }

    public class VeiculoUpdateDto : VeiculoCreateDto
    {
        public bool Disponivel { get; set; } = true;
    }

    public class VeiculoReadDto
    {
        public int VeiculoId { get; set; }
        public string Modelo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public int AnoFabricacao { get; set; }
        public int Quilometragem { get; set; }
        public bool Disponivel { get; set; }

        public int FabricanteId { get; set; }
        public string FabricanteNome { get; set; } = string.Empty;

        public int CategoriaVeiculoId { get; set; }
        public string CategoriaVeiculoNome { get; set; } = string.Empty;
        public decimal ValorDiariaBase { get; set; }
    }
}
