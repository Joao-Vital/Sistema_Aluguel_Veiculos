using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocadoraVeiculos.API.Models
{
    public class Aluguel
    {
        [Key]
        public int AluguelId { get; set; }

        // FK - Cliente (obrigatório: todo aluguel está atrelado a um cliente)
        [Required]
        [ForeignKey(nameof(Cliente))]
        public int ClienteId { get; set; }
        public Cliente Cliente { get; set; } = null!;

        // FK - Veiculo (obrigatório: todo aluguel está atrelado a um veículo)
        [Required]
        [ForeignKey(nameof(Veiculo))]
        public int VeiculoId { get; set; }
        public Veiculo Veiculo { get; set; } = null!;

        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataFimPrevista { get; set; }

        /// Data em que o veículo foi efetivamente devolvido. Nula enquanto o aluguel está em aberto.
        public DateTime? DataDevolucao { get; set; }

        [Required]
        public int QuilometragemInicial { get; set; }

        /// Nula até a devolução do veículo.
        public int? QuilometragemFinal { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorDiaria { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal ValorTotal { get; set; }
    }
}
