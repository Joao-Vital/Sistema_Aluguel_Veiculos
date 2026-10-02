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

        [HttpGet]
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

        [HttpGet("{id:int}")]
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

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
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

        [HttpPut("{id:int}")]
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

        [HttpDelete("{id:int}")]
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
