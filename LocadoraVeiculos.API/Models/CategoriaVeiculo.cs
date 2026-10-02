using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.API.Models
{
    /// Entidade adicional (5ª entidade, exigida pelo item 1.5).
    /// Classifica o veículo (Popular, Intermediário, SUV, Luxo etc.) e
    /// define um valor de diária sugerido/base para a categoria.
    public class CategoriaVeiculo
    {
        [Key]
        public int CategoriaVeiculoId { get; set; }

        [Required]
        [StringLength(50)]
        public string Nome { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descricao { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorDiariaBase { get; set; }

        // Navegação: uma categoria agrupa muitos veículos
        public ICollection<Veiculo> Veiculos { get; set; } = new List<Veiculo>();
    }
}
