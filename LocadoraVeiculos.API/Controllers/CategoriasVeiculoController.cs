using LocadoraVeiculos.API.Data;
using LocadoraVeiculos.API.DTOs;
using LocadoraVeiculos.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocadoraVeiculos.API.Controllers
{
    [ApiController]
    [Route("api/categorias-veiculo")]
    [Produces("application/json")]
    public class CategoriasVeiculoController : ControllerBase
    {
        private readonly LocadoraContext _context;

        public CategoriasVeiculoController(LocadoraContext context)
        {
            _context = context;
        }

        /// <summary>Lista todas as categorias de veículo (ex.: Popular, SUV, Luxo).</summary>
        /// <response code="200">Lista de categorias retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoriaVeiculoReadDto>>> GetAll()
        {
            var categorias = await _context.CategoriasVeiculo
                .AsNoTracking()
                .Select(c => new CategoriaVeiculoReadDto
                {
                    CategoriaVeiculoId = c.CategoriaVeiculoId,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    ValorDiariaBase = c.ValorDiariaBase,
                    QuantidadeVeiculos = c.Veiculos.Count
                })
                .ToListAsync();

            return Ok(categorias);
        }

        /// <summary>Busca uma categoria de veículo pelo id.</summary>
        /// <param name="id">Id da categoria.</param>
        /// <response code="200">Categoria encontrada.</response>
        /// <response code="404">Nenhuma categoria com esse id.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoriaVeiculoReadDto>> GetById(int id)
        {
            var categoria = await _context.CategoriasVeiculo
                .AsNoTracking()
                .Where(c => c.CategoriaVeiculoId == id)
                .Select(c => new CategoriaVeiculoReadDto
                {
                    CategoriaVeiculoId = c.CategoriaVeiculoId,
                    Nome = c.Nome,
                    Descricao = c.Descricao,
                    ValorDiariaBase = c.ValorDiariaBase,
                    QuantidadeVeiculos = c.Veiculos.Count
                })
                .FirstOrDefaultAsync();

            if (categoria == null)
                return NotFound(new { mensagem = $"Categoria com id {id} não encontrada." });

            return Ok(categoria);
        }

        /// <summary>Cadastra uma nova categoria de veículo.</summary>
        /// <param name="dto">Nome, descrição (opcional) e valor da diária base.</param>
        /// <response code="201">Categoria criada com sucesso.</response>
        /// <response code="400">Dados inválidos (ex.: valor da diária menor ou igual a zero).</response>
        /// <response code="409">Já existe uma categoria com esse nome.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<CategoriaVeiculoReadDto>> Create(CategoriaVeiculoCreateDto dto)
        {
            var nomeExiste = await _context.CategoriasVeiculo.AnyAsync(c => c.Nome == dto.Nome);
            if (nomeExiste)
                return Conflict(new { mensagem = $"Já existe uma categoria chamada '{dto.Nome}'." });

            var categoria = new CategoriaVeiculo
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                ValorDiariaBase = dto.ValorDiariaBase
            };

            _context.CategoriasVeiculo.Add(categoria);
            await _context.SaveChangesAsync();

            var readDto = new CategoriaVeiculoReadDto
            {
                CategoriaVeiculoId = categoria.CategoriaVeiculoId,
                Nome = categoria.Nome,
                Descricao = categoria.Descricao,
                ValorDiariaBase = categoria.ValorDiariaBase,
                QuantidadeVeiculos = 0
            };

            return CreatedAtAction(nameof(GetById), new { id = categoria.CategoriaVeiculoId }, readDto);
        }

        /// <summary>Atualiza os dados de uma categoria existente.</summary>
        /// <param name="id">Id da categoria a atualizar.</param>
        /// <param name="dto">Novos dados da categoria.</param>
        /// <response code="204">Atualizada com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="404">Categoria não encontrada.</response>
        /// <response code="409">Já existe outra categoria com esse nome.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, CategoriaVeiculoCreateDto dto)
        {
            var categoria = await _context.CategoriasVeiculo.FindAsync(id);
            if (categoria == null)
                return NotFound(new { mensagem = $"Categoria com id {id} não encontrada." });

            var nomeEmUsoPorOutro = await _context.CategoriasVeiculo
                .AnyAsync(c => c.Nome == dto.Nome && c.CategoriaVeiculoId != id);
            if (nomeEmUsoPorOutro)
                return Conflict(new { mensagem = $"Já existe uma categoria chamada '{dto.Nome}'." });

            categoria.Nome = dto.Nome;
            categoria.Descricao = dto.Descricao;
            categoria.ValorDiariaBase = dto.ValorDiariaBase;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Exclui uma categoria (somente se não houver veículos vinculados a ela).</summary>
        /// <param name="id">Id da categoria a excluir.</param>
        /// <response code="204">Excluída com sucesso.</response>
        /// <response code="404">Categoria não encontrada.</response>
        /// <response code="409">Existem veículos cadastrados nessa categoria.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var categoria = await _context.CategoriasVeiculo.FindAsync(id);
            if (categoria == null)
                return NotFound(new { mensagem = $"Categoria com id {id} não encontrada." });

            var possuiVeiculos = await _context.Veiculos.AnyAsync(v => v.CategoriaVeiculoId == id);
            if (possuiVeiculos)
                return Conflict(new { mensagem = "Não é possível excluir uma categoria que possui veículos cadastrados." });

            _context.CategoriasVeiculo.Remove(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
