using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.API.Models
{
    public class Cliente
    {
        [Key]
        public int ClienteId { get; set; }

        [Required]
        [StringLength(120)]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string CPF { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(120)]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Telefone { get; set; }

        // Navegação: um cliente pode ter vários aluguéis
        public ICollection<Aluguel> Alugueis { get; set; } = new List<Aluguel>();
    }
}
