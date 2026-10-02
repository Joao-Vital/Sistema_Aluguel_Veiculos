using LocadoraVeiculos.API.Data;
using LocadoraVeiculos.API.DTOs;
using LocadoraVeiculos.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocadoraVeiculos.API.Controllers
{
    [ApiController]
    [Route("api/alugueis")]
    [Produces("application/json")]
    public class AlugueisController : ControllerBase
    {
        private readonly LocadoraContext _context;

        public AlugueisController(LocadoraContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // CRUD
        // ---------------------------------------------------------------

        // GET api/alugueis
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AluguelReadDto>>> GetAll()
        {
            var alugueis = await _context.Alugueis
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Veiculo).ThenInclude(v => v.Fabricante)
                .OrderByDescending(a => a.DataInicio)
                .ToListAsync();

            return Ok(alugueis.Select(MapToReadDto));
        }

        // GET api/alugueis/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<AluguelReadDto>> GetById(int id)
        {
            var aluguel = await _context.Alugueis
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Veiculo).ThenInclude(v => v.Fabricante)
                .Where(a => a.AluguelId == id)
                .FirstOrDefaultAsync();

            if (aluguel == null)
                return NotFound(new { mensagem = $"Aluguel com id {id} não encontrado." });

            return Ok(MapToReadDto(aluguel));
        }

        // POST api/alugueis — abre um novo aluguel
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AluguelReadDto>> Create(AluguelCreateDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return NotFound(new { mensagem = $"Cliente com id {dto.ClienteId} não encontrado." });

            var veiculo = await _context.Veiculos
                .Include(v => v.CategoriaVeiculo)
                .FirstOrDefaultAsync(v => v.VeiculoId == dto.VeiculoId);
            if (veiculo == null)
                return NotFound(new { mensagem = $"Veículo com id {dto.VeiculoId} não encontrado." });

            if (!veiculo.Disponivel)
                return Conflict(new { mensagem = $"O veículo '{veiculo.Modelo}' (placa {veiculo.Placa}) não está disponível para locação no momento." });

            var valorDiaria = dto.ValorDiaria ?? veiculo.CategoriaVeiculo.ValorDiariaBase;
            var dias = CalcularDias(dto.DataInicio, dto.DataFimPrevista);

            var aluguel = new Aluguel
            {
                ClienteId = dto.ClienteId,
                VeiculoId = dto.VeiculoId,
                DataInicio = dto.DataInicio,
                DataFimPrevista = dto.DataFimPrevista,
                QuilometragemInicial = veiculo.Quilometragem,
                ValorDiaria = valorDiaria,
                ValorTotal = valorDiaria * dias
            };

            veiculo.Disponivel = false;

            _context.Alugueis.Add(aluguel);
            await _context.SaveChangesAsync();

            var criado = await _context.Alugueis
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Veiculo).ThenInclude(v => v.Fabricante)
                .Where(a => a.AluguelId == aluguel.AluguelId)
                .FirstAsync();

            return CreatedAtAction(nameof(GetById), new { id = aluguel.AluguelId }, MapToReadDto(criado));
        }

        // PATCH api/alugueis/5/devolucao — registra a devolução do veículo
        [HttpPatch("{id:int}/devolucao")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AluguelReadDto>> RegistrarDevolucao(int id, AluguelDevolucaoDto dto)
        {
            var aluguel = await _context.Alugueis
                .Include(a => a.Veiculo)
                .FirstOrDefaultAsync(a => a.AluguelId == id);

            if (aluguel == null)
                return NotFound(new { mensagem = $"Aluguel com id {id} não encontrado." });

            if (aluguel.DataDevolucao != null)
                return Conflict(new { mensagem = "Este aluguel já teve sua devolução registrada." });

            if (dto.QuilometragemFinal < aluguel.QuilometragemInicial)
                return BadRequest(new { mensagem = $"A quilometragem final ({dto.QuilometragemFinal}) não pode ser menor que a quilometragem inicial ({aluguel.QuilometragemInicial})." });

            var dataDevolucao = dto.DataDevolucao ?? DateTime.Now;
            if (dataDevolucao < aluguel.DataInicio)
                return BadRequest(new { mensagem = "A data de devolução não pode ser anterior à data de início do aluguel." });

            aluguel.DataDevolucao = dataDevolucao;
            aluguel.QuilometragemFinal = dto.QuilometragemFinal;

            var diasEfetivos = CalcularDias(aluguel.DataInicio, dataDevolucao);
            aluguel.ValorTotal = aluguel.ValorDiaria * diasEfetivos;

            aluguel.Veiculo.Quilometragem = dto.QuilometragemFinal;
            aluguel.Veiculo.Disponivel = true;

            await _context.SaveChangesAsync();

            var atualizado = await _context.Alugueis
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Veiculo).ThenInclude(v => v.Fabricante)
                .Where(a => a.AluguelId == id)
                .FirstAsync();

            return Ok(MapToReadDto(atualizado));
        }

        // DELETE api/alugueis/5 — cancela um aluguel ainda em aberto
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var aluguel = await _context.Alugueis
                .Include(a => a.Veiculo)
                .FirstOrDefaultAsync(a => a.AluguelId == id);

            if (aluguel == null)
                return NotFound(new { mensagem = $"Aluguel com id {id} não encontrado." });

            if (aluguel.DataDevolucao != null)
                return Conflict(new { mensagem = "Não é possível excluir um aluguel que já foi finalizado (devolvido). Isso removeria o histórico da locação." });

            aluguel.Veiculo.Disponivel = true;

            _context.Alugueis.Remove(aluguel);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ---------------------------------------------------------------
        // FILTROS (item 2.5)
        // ---------------------------------------------------------------

        /// <summary>
        /// Filtro 4: todos os aluguéis de um cliente, com dados do veículo e do fabricante.
        /// JOIN explícito (cláusula "join" do LINQ) encadeando três tabelas — INNER JOIN.
        /// </summary>
        [HttpGet("filtro/cliente/{clienteId:int}")]
        public async Task<ActionResult<IEnumerable<AluguelReadDto>>> FiltroPorCliente(int clienteId)
        {
            var clienteExiste = await _context.Clientes.AnyAsync(c => c.ClienteId == clienteId);
            if (!clienteExiste)
                return NotFound(new { mensagem = $"Cliente com id {clienteId} não encontrado." });

            var alugueis = await (
                from a in _context.Alugueis
                join c in _context.Clientes on a.ClienteId equals c.ClienteId
                join v in _context.Veiculos on a.VeiculoId equals v.VeiculoId
                join f in _context.Fabricantes on v.FabricanteId equals f.FabricanteId
                where c.ClienteId == clienteId
                orderby a.DataInicio descending
                select new AluguelReadDto
                {
                    AluguelId = a.AluguelId,
                    ClienteId = c.ClienteId,
                    ClienteNome = c.Nome,
                    VeiculoId = v.VeiculoId,
                    VeiculoModelo = v.Modelo,
                    VeiculoPlaca = v.Placa,
                    FabricanteNome = f.Nome,
                    DataInicio = a.DataInicio,
                    DataFimPrevista = a.DataFimPrevista,
                    DataDevolucao = a.DataDevolucao,
                    QuilometragemInicial = a.QuilometragemInicial,
                    QuilometragemFinal = a.QuilometragemFinal,
                    ValorDiaria = a.ValorDiaria,
                    ValorTotal = a.ValorTotal
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(alugueis);
        }

        /// <summary>
        /// Filtro 5: aluguéis iniciados dentro de um período (inclusive), com dados de cliente e veículo.
        /// JOIN por navegação (Include) — INNER JOIN entre Aluguel, Cliente e Veiculo/Fabricante.
        /// </summary>
        [HttpGet("filtro/periodo")]
        public async Task<ActionResult<IEnumerable<AluguelReadDto>>> FiltroPorPeriodo([FromQuery] DateTime inicio, [FromQuery] DateTime fim)
        {
            if (fim < inicio)
                return BadRequest(new { mensagem = "A data 'fim' não pode ser anterior à data 'inicio'." });

            var alugueis = await _context.Alugueis
                .AsNoTracking()
                .Include(a => a.Cliente)
                .Include(a => a.Veiculo).ThenInclude(v => v.Fabricante)
                .Where(a => a.DataInicio.Date >= inicio.Date && a.DataInicio.Date <= fim.Date)
                .OrderBy(a => a.DataInicio)
                .ToListAsync();

            return Ok(alugueis.Select(MapToReadDto));
        }

        // ---------------------------------------------------------------
        // Auxiliares
        // ---------------------------------------------------------------

        /// <summary>
        /// Calcula a quantidade de diárias cobradas entre duas datas, sempre arredondando
        /// para cima e cobrando o mínimo de 1 diária.
        /// </summary>
        private static int CalcularDias(DateTime inicio, DateTime fim)
        {
            var dias = (int)Math.Ceiling((fim - inicio).TotalDays);
            return Math.Max(dias, 1);
        }

        private static AluguelReadDto MapToReadDto(Aluguel a) => new()
        {
            AluguelId = a.AluguelId,
            ClienteId = a.ClienteId,
            ClienteNome = a.Cliente.Nome,
            VeiculoId = a.VeiculoId,
            VeiculoModelo = a.Veiculo.Modelo,
            VeiculoPlaca = a.Veiculo.Placa,
            FabricanteNome = a.Veiculo.Fabricante.Nome,
            DataInicio = a.DataInicio,
            DataFimPrevista = a.DataFimPrevista,
            DataDevolucao = a.DataDevolucao,
            QuilometragemInicial = a.QuilometragemInicial,
            QuilometragemFinal = a.QuilometragemFinal,
            ValorDiaria = a.ValorDiaria,
            ValorTotal = a.ValorTotal
        };
    }
}
