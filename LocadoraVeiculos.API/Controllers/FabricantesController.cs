using LocadoraVeiculos.API.Data;
using LocadoraVeiculos.API.DTOs;
using LocadoraVeiculos.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocadoraVeiculos.API.Controllers
{
    [ApiController]
    [Route("api/fabricantes")]
    [Produces("application/json")]
    public class FabricantesController : ControllerBase
    {
        private readonly LocadoraContext _context;

        public FabricantesController(LocadoraContext context)
        {
            _context = context;
        }

        // GET api/fabricantes
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<FabricanteReadDto>>> GetAll()
        {
            var fabricantes = await _context.Fabricantes
                .AsNoTracking()
                .Select(f => new FabricanteReadDto
                {
                    FabricanteId = f.FabricanteId,
                    Nome = f.Nome,
                    PaisOrigem = f.PaisOrigem,
                    QuantidadeVeiculos = f.Veiculos.Count
                })
                .ToListAsync();

            return Ok(fabricantes);
        }

        // GET api/fabricantes/5
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FabricanteReadDto>> GetById(int id)
        {
            var fabricante = await _context.Fabricantes
                .AsNoTracking()
                .Where(f => f.FabricanteId == id)
                .Select(f => new FabricanteReadDto
                {
                    FabricanteId = f.FabricanteId,
                    Nome = f.Nome,
                    PaisOrigem = f.PaisOrigem,
                    QuantidadeVeiculos = f.Veiculos.Count
                })
                .FirstOrDefaultAsync();

            if (fabricante == null)
                return NotFound(new { mensagem = $"Fabricante com id {id} não encontrado." });

            return Ok(fabricante);
        }

        // POST api/fabricantes
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<FabricanteReadDto>> Create(FabricanteCreateDto dto)
        {
            var nomeExiste = await _context.Fabricantes.AnyAsync(f => f.Nome == dto.Nome);
            if (nomeExiste)
                return Conflict(new { mensagem = $"Já existe um fabricante chamado '{dto.Nome}'." });

            var fabricante = new Fabricante
            {
                Nome = dto.Nome,
                PaisOrigem = dto.PaisOrigem
            };

            _context.Fabricantes.Add(fabricante);
            await _context.SaveChangesAsync();

            var readDto = new FabricanteReadDto
            {
                FabricanteId = fabricante.FabricanteId,
                Nome = fabricante.Nome,
                PaisOrigem = fabricante.PaisOrigem,
                QuantidadeVeiculos = 0
            };

            return CreatedAtAction(nameof(GetById), new { id = fabricante.FabricanteId }, readDto);
        }

        // PUT api/fabricantes/5
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(int id, FabricanteCreateDto dto)
        {
            var fabricante = await _context.Fabricantes.FindAsync(id);
            if (fabricante == null)
                return NotFound(new { mensagem = $"Fabricante com id {id} não encontrado." });

            var nomeEmUsoPorOutro = await _context.Fabricantes
                .AnyAsync(f => f.Nome == dto.Nome && f.FabricanteId != id);
            if (nomeEmUsoPorOutro)
                return Conflict(new { mensagem = $"Já existe um fabricante chamado '{dto.Nome}'." });

            fabricante.Nome = dto.Nome;
            fabricante.PaisOrigem = dto.PaisOrigem;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/fabricantes/5
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(int id)
        {
            var fabricante = await _context.Fabricantes.FindAsync(id);
            if (fabricante == null)
                return NotFound(new { mensagem = $"Fabricante com id {id} não encontrado." });

            var possuiVeiculos = await _context.Veiculos.AnyAsync(v => v.FabricanteId == id);
            if (possuiVeiculos)
                return Conflict(new { mensagem = "Não é possível excluir um fabricante que possui veículos cadastrados." });

            _context.Fabricantes.Remove(fabricante);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
