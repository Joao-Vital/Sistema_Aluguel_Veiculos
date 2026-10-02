using LocadoraVeiculos.API.Data;
using LocadoraVeiculos.API.DTOs;
using LocadoraVeiculos.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocadoraVeiculos.API.Controllers
{
    [ApiController]
    [Route("api/veiculos")]
    [Produces("application/json")]
    public class VeiculosController : ControllerBase
    {
        private readonly LocadoraContext _context;

        public VeiculosController(LocadoraContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------------
        // CRUD
        // ---------------------------------------------------------------

        // GET api/veiculos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VeiculoReadDto>>> GetAll()
        {
            var veiculos = await _context.Veiculos
                .AsNoTracking()
                .Include(v => v.Fabricante)
                .Include(v => v.CategoriaVeiculo)
                .ToListAsync();

            return Ok(veiculos.Select(MapToReadDto));
        }

        // GET api/veiculos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<VeiculoReadDto>> GetById(int id)
        {
            var veiculo = await _context.Veiculos
                .AsNoTracking()
                .Include(v => v.Fabricante)
                .Include(v => v.CategoriaVeiculo)
                .Where(v => v.VeiculoId == id)
                .FirstOrDefaultAsync();

            if (veiculo == null)
                return NotFound(new { mensagem = $"Veículo com id {id} não encontrado." });

            return Ok(MapToReadDto(veiculo));
        }

        // POST api/veiculos
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<VeiculoReadDto>> Create(VeiculoCreateDto dto)
        {
            var erro = await ValidarReferencias(dto.FabricanteId, dto.CategoriaVeiculoId);
            if (erro != null)
                return NotFound(new { mensagem = erro });

            var placaEmUso = await _context.Veiculos.AnyAsync(v => v.Placa == dto.Placa);
            if (placaEmUso)
                return Conflict(new { mensagem = $"Já existe um veículo cadastrado com a placa '{dto.Placa}'." });

            var veiculo = new Veiculo
            {
                Modelo = dto.Modelo,
                Placa = dto.Placa,
                AnoFabricacao = dto.AnoFabricacao,
                Quilometragem = dto.Quilometragem,
                FabricanteId = dto.FabricanteId,
                CategoriaVeiculoId = dto.CategoriaVeiculoId,
                Disponivel = true
            };

            _context.Veiculos.Add(veiculo);
            await _context.SaveChangesAsync();

            return await BuscarERetornarCriado(veiculo.VeiculoId);
        }

        // PUT api/veiculos/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, VeiculoUpdateDto dto)
        {
            var veiculo = await _context.Veiculos.FindAsync(id);
            if (veiculo == null)
                return NotFound(new { mensagem = $"Veículo com id {id} não encontrado." });

            var erro = await ValidarReferencias(dto.FabricanteId, dto.CategoriaVeiculoId);
            if (erro != null)
                return NotFound(new { mensagem = erro });

            var placaEmUsoPorOutro = await _context.Veiculos
                .AnyAsync(v => v.Placa == dto.Placa && v.VeiculoId != id);
            if (placaEmUsoPorOutro)
                return Conflict(new { mensagem = $"Já existe um veículo cadastrado com a placa '{dto.Placa}'." });

            veiculo.Modelo = dto.Modelo;
            veiculo.Placa = dto.Placa;
            veiculo.AnoFabricacao = dto.AnoFabricacao;
            veiculo.Quilometragem = dto.Quilometragem;
            veiculo.FabricanteId = dto.FabricanteId;
            veiculo.CategoriaVeiculoId = dto.CategoriaVeiculoId;
            veiculo.Disponivel = dto.Disponivel;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/veiculos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var veiculo = await _context.Veiculos.FindAsync(id);
            if (veiculo == null)
                return NotFound(new { mensagem = $"Veículo com id {id} não encontrado." });

            var possuiAlugueis = await _context.Alugueis.AnyAsync(a => a.VeiculoId == id);
            if (possuiAlugueis)
                return Conflict(new { mensagem = "Não é possível excluir um veículo que possui aluguéis registrados." });

            _context.Veiculos.Remove(veiculo);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ---------------------------------------------------------------
        // FILTROS — cada um usa uma técnica de JOIN diferente
        // ---------------------------------------------------------------

        /// <summary>
        /// Filtro 1: veículos disponíveis de uma determinada categoria.
        /// JOIN por navegação (Include) — o EF traduz para INNER JOIN entre Veiculo, Fabricante e CategoriaVeiculo.
        /// </summary>
        [HttpGet("filtro/disponiveis-por-categoria/{categoriaId:int}")]
        public async Task<ActionResult<IEnumerable<VeiculoReadDto>>> FiltroDisponiveisPorCategoria(int categoriaId)
        {
            var categoriaExiste = await _context.CategoriasVeiculo.AnyAsync(c => c.CategoriaVeiculoId == categoriaId);
            if (!categoriaExiste)
                return NotFound(new { mensagem = $"Categoria com id {categoriaId} não encontrada." });

            var veiculos = await _context.Veiculos
                .AsNoTracking()
                .Include(v => v.Fabricante)
                .Include(v => v.CategoriaVeiculo)
                .Where(v => v.CategoriaVeiculoId == categoriaId && v.Disponivel)
                .ToListAsync();

            return Ok(veiculos.Select(MapToReadDto));
        }

        /// <summary>
        /// Filtro 2: veículos de um fabricante, com a quantidade de aluguéis e o total de dias alugados.
        /// JOIN explícito (cláusula "join" do LINQ) — INNER JOIN entre Veiculo e Aluguel.
        /// </summary>
        [HttpGet("filtro/fabricante/{fabricanteId:int}")]
        public async Task<ActionResult<IEnumerable<object>>> FiltroPorFabricanteComEstatisticas(int fabricanteId)
        {
            var fabricanteExiste = await _context.Fabricantes.AnyAsync(f => f.FabricanteId == fabricanteId);
            if (!fabricanteExiste)
                return NotFound(new { mensagem = $"Fabricante com id {fabricanteId} não encontrado." });

            var resultado = await (
                from v in _context.Veiculos
                join a in _context.Alugueis on v.VeiculoId equals a.VeiculoId
                where v.FabricanteId == fabricanteId
                group a by new { v.VeiculoId, v.Modelo, v.Placa } into g
                select new
                {
                    g.Key.VeiculoId,
                    g.Key.Modelo,
                    g.Key.Placa,
                    QuantidadeAlugueis = g.Count(),
                    ValorTotalArrecadado = g.Sum(a => a.ValorTotal)
                })
                .AsNoTracking()
                .ToListAsync();

            return Ok(resultado);
        }

        /// <summary>
        /// Filtro 3: veículos que nunca foram alugados (nenhum registro em Aluguel).
        /// JOIN externo (GroupJoin + DefaultIfEmpty) — equivalente a um LEFT JOIN entre Veiculo e Aluguel.
        /// </summary>
        [HttpGet("filtro/sem-aluguel")]
        public async Task<ActionResult<IEnumerable<VeiculoReadDto>>> FiltroVeiculosSemAluguel()
        {
            var veiculosSemAluguel = await (
                from v in _context.Veiculos
                    .AsNoTracking()
                    .Include(v => v.Fabricante)
                    .Include(v => v.CategoriaVeiculo)
                join a in _context.Alugueis on v.VeiculoId equals a.VeiculoId into alugueisDoVeiculo
                from a in alugueisDoVeiculo.DefaultIfEmpty()
                where a == null
                select v)
                .ToListAsync();

            return Ok(veiculosSemAluguel.Select(MapToReadDto));
        }

        // ---------------------------------------------------------------
        // Auxiliares
        // ---------------------------------------------------------------

        private async Task<string?> ValidarReferencias(int fabricanteId, int categoriaVeiculoId)
        {
            var fabricanteExiste = await _context.Fabricantes.AnyAsync(f => f.FabricanteId == fabricanteId);
            if (!fabricanteExiste)
                return $"Fabricante com id {fabricanteId} não encontrado.";

            var categoriaExiste = await _context.CategoriasVeiculo.AnyAsync(c => c.CategoriaVeiculoId == categoriaVeiculoId);
            if (!categoriaExiste)
                return $"Categoria de veículo com id {categoriaVeiculoId} não encontrada.";

            return null;
        }

        private async Task<ActionResult<VeiculoReadDto>> BuscarERetornarCriado(int veiculoId)
        {
            var criado = await _context.Veiculos
                .AsNoTracking()
                .Include(v => v.Fabricante)
                .Include(v => v.CategoriaVeiculo)
                .Where(v => v.VeiculoId == veiculoId)
                .FirstAsync();

            return CreatedAtAction(nameof(GetById), new { id = veiculoId }, MapToReadDto(criado));
        }

        private static VeiculoReadDto MapToReadDto(Veiculo v) => new()
        {
            VeiculoId = v.VeiculoId,
            Modelo = v.Modelo,
            Placa = v.Placa,
            AnoFabricacao = v.AnoFabricacao,
            Quilometragem = v.Quilometragem,
            Disponivel = v.Disponivel,
            FabricanteId = v.FabricanteId,
            FabricanteNome = v.Fabricante.Nome,
            CategoriaVeiculoId = v.CategoriaVeiculoId,
            CategoriaVeiculoNome = v.CategoriaVeiculo.Nome,
            ValorDiariaBase = v.CategoriaVeiculo.ValorDiariaBase
        };
    }
}
