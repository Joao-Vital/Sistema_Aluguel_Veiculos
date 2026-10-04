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

        /// <summary>Lista todos os veículos cadastrados, com dados de fabricante e categoria.</summary>
        /// <response code="200">Lista de veículos retornada com sucesso.</response>
        // GET api/veiculos
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<VeiculoReadDto>>> GetAll()
        {
            var veiculos = await _context.Veiculos
                .AsNoTracking()
                .Include(v => v.Fabricante)
                .Include(v => v.CategoriaVeiculo)
                .ToListAsync();

            return Ok(veiculos.Select(MapToReadDto));
        }

        /// <summary>Busca um veículo pelo id.</summary>
        /// <param name="id">Id do veículo.</param>
        /// <response code="200">Veículo encontrado.</response>
        /// <response code="404">Nenhum veículo com esse id.</response>
        // GET api/veiculos/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>Cadastra um novo veículo.</summary>
        /// <param name="dto">Modelo, placa (única), ano, quilometragem, fabricante e categoria (devem existir).</param>
        /// <response code="201">Veículo criado com sucesso.</response>
        /// <response code="400">Dados inválidos (ex.: ano fora do intervalo aceito).</response>
        /// <response code="404">Fabricante ou categoria informados não existem.</response>
        /// <response code="409">Já existe um veículo com essa placa.</response>
        // POST api/veiculos
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>Atualiza os dados de um veículo existente, incluindo disponibilidade.</summary>
        /// <param name="id">Id do veículo a atualizar.</param>
        /// <param name="dto">Novos dados do veículo.</param>
        /// <response code="204">Atualizado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="404">Veículo, fabricante ou categoria não encontrados.</response>
        /// <response code="409">Já existe outro veículo com essa placa.</response>
        // PUT api/veiculos/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>Exclui um veículo (somente se não houver aluguéis vinculados a ele).</summary>
        /// <param name="id">Id do veículo a excluir.</param>
        /// <response code="204">Excluído com sucesso.</response>
        /// <response code="404">Veículo não encontrado.</response>
        /// <response code="409">Existem aluguéis registrados para esse veículo.</response>
        // DELETE api/veiculos/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
        /// <param name="categoriaId">Id da categoria de veículo.</param>
        /// <response code="200">Lista de veículos disponíveis dessa categoria.</response>
        /// <response code="404">Categoria não encontrada.</response>
        [HttpGet("filtro/disponiveis-por-categoria/{categoriaId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// <param name="fabricanteId">Id do fabricante.</param>
        /// <response code="200">Estatísticas de aluguel por veículo do fabricante.</response>
        /// <response code="404">Fabricante não encontrado.</response>
        [HttpGet("filtro/fabricante/{fabricanteId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        /// <response code="200">Lista de veículos sem nenhum aluguel (pode vir vazia).</response>
        [HttpGet("filtro/sem-aluguel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
