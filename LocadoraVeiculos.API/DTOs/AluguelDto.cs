using System.ComponentModel.DataAnnotations;

namespace LocadoraVeiculos.API.DTOs
{
    public class AluguelCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "O cliente é obrigatório.")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "O veículo é obrigatório.")]
        public int VeiculoId { get; set; }

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        public DateTime DataInicio { get; set; }

        [Required(ErrorMessage = "A data prevista para devolução é obrigatória.")]
        public DateTime DataFimPrevista { get; set; }

        /// <summary>
        /// Se não informado, o valor da diária da categoria do veículo é usado como padrão.
        /// </summary>
        [Range(0.01, 100000, ErrorMessage = "O valor da diária deve ser maior que zero.")]
        public decimal? ValorDiaria { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (DataFimPrevista <= DataInicio)
            {
                yield return new ValidationResult(
                    "A data prevista para devolução deve ser posterior à data de início.",
                    new[] { nameof(DataFimPrevista) });
            }
        }
    }

    public class AluguelDevolucaoDto
    {
        [Required(ErrorMessage = "A quilometragem final é obrigatória.")]
        [Range(0, int.MaxValue, ErrorMessage = "A quilometragem final não pode ser negativa.")]
        public int QuilometragemFinal { get; set; }

        /// <summary>
        /// Opcional. Se não informado, é usada a data/hora atual do servidor.
        /// </summary>
        public DateTime? DataDevolucao { get; set; }
    }

    public class AluguelReadDto
    {
        public int AluguelId { get; set; }

        public int ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;

        public int VeiculoId { get; set; }
        public string VeiculoModelo { get; set; } = string.Empty;
        public string VeiculoPlaca { get; set; } = string.Empty;
        public string FabricanteNome { get; set; } = string.Empty;

        public DateTime DataInicio { get; set; }
        public DateTime DataFimPrevista { get; set; }
        public DateTime? DataDevolucao { get; set; }

        public int QuilometragemInicial { get; set; }
        public int? QuilometragemFinal { get; set; }

        public decimal ValorDiaria { get; set; }
        public decimal ValorTotal { get; set; }

        public bool EmAberto => DataDevolucao == null;
        public bool EmAtraso => DataDevolucao == null && DateTime.Now > DataFimPrevista;
    }
}
