using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.API.Models
{
    
    /// Representa a marca/montadora do veículo (ex: Chevrolet, Fiat, Toyota).
    
    public class Fabricante
    {
        [Key]
        public int FabricanteId { get; set; }

        [Required]
        [StringLength(80)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(60)]
        public string? PaisOrigem { get; set; }

        // Navegação: um fabricante possui muitos veículos
        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    }
}
