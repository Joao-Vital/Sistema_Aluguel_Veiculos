using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.API.DTOs
{
    public class FabricanteCreateDto
    {
        [Required(ErrorMessage = "O nome do fabricante é obrigatório.")]
        [StringLength(80, MinimumLength = 2)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(60)]
        public string? PaisOrigem { get; set; }
    }

    public class FabricanteReadDto
    {
        public int FabricanteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? PaisOrigem { get; set; }
        public int QuantidadeVeiculos { get; set; }
    }
}
