using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.API.Models
{
    public class Veiculo
    {
        [Key]
        public int VeiculoId { get; set; }

        [Required]
        [StringLength(60)]
        public string Modelo { get; set; } = string.Empty;

        [Required]
        [StringLength(8)]
        public string Placa { get; set; } = string.Empty;

        [Required]
        public int AnoFabricacao { get; set; }

        /// Quilometragem atual/geral do veículo (independe do aluguel específico).
        public int Quilometragem { get; set; }

        public bool Disponivel { get; set; } = true;

        // FK - Fabricante (obrigatório: todo veículo pertence a um fabricante)
        [Required]
        [ForeignKey(nameof(Fabricante))]
        public int FabricanteId { get; set; }
        public Fabricante Fabricante { get; set; } = null!;

        // FK - CategoriaVeiculo
        [Required]
        [ForeignKey(nameof(CategoriaVeiculo))]
        public int CategoriaVeiculoId { get; set; }
        public CategoriaVeiculo CategoriaVeiculo { get; set; } = null!;

        // Navegação: um veículo pode ter vários aluguéis ao longo do tempo
        public ICollection<Aluguel> Alugueis { get; set; } = new List<Aluguel>();
    }
}
