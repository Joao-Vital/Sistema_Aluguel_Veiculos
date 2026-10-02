using LocadoraVeiculos.API.Data;
using LocadoraVeiculos.API.DTOs;
using LocadoraVeiculos.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocadoraVeiculos.API.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    [Produces("application/json")]
    public class ClientesController : ControllerBase
    {
        private readonly LocadoraContext _context;

        public ClientesController(LocadoraContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClienteReadDto>>> GetAll()
        {
            var clientes = await _context.Clientes
                .AsNoTracking()
                .Select(c => new ClienteReadDto
                {
                    ClienteId = c.ClienteId,
                    Nome = c.Nome,
                    CPF = c.CPF,
                    Email = c.Email,
                    Telefone = c.Telefone,
                    QuantidadeAlugueis = c.Alugueis.Count
                })
                .ToListAsync();

            return Ok(clientes);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ClienteReadDto>> GetById(int id)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .Where(c => c.ClienteId == id)
                .Select(c => new ClienteReadDto
                {
                    ClienteId = c.ClienteId,
                    Nome = c.Nome,
                    CPF = c.CPF,
                    Email = c.Email,
                    Telefone = c.Telefone,
                    QuantidadeAlugueis = c.Alugueis.Count
                })
                .FirstOrDefaultAsync();

            if (cliente == null)
                return NotFound(new { mensagem = $"Cliente com id {id} não encontrado." });

            return Ok(cliente);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<ActionResult<ClienteReadDto>> Create(ClienteCreateDto dto)
        {
            var conflito = await ValidarCpfEmailUnicos(dto.CPF, dto.Email);
            if (conflito != null)
                return Conflict(new { mensagem = conflito });

            var cliente = new Cliente
            {
                Nome = dto.Nome,
                CPF = dto.CPF,
                Email = dto.Email,
                Telefone = dto.Telefone
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            var readDto = new ClienteReadDto
            {
                ClienteId = cliente.ClienteId,
                Nome = cliente.Nome,
                CPF = cliente.CPF,
                Email = cliente.Email,
                Telefone = cliente.Telefone,
                QuantidadeAlugueis = 0
            };

            return CreatedAtAction(nameof(GetById), new { id = cliente.ClienteId }, readDto);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, ClienteUpdateDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound(new { mensagem = $"Cliente com id {id} não encontrado." });

            var conflito = await ValidarCpfEmailUnicos(dto.CPF, dto.Email, id);
            if (conflito != null)
                return Conflict(new { mensagem = conflito });

            cliente.Nome = dto.Nome;
            cliente.CPF = dto.CPF;
            cliente.Email = dto.Email;
            cliente.Telefone = dto.Telefone;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
                return NotFound(new { mensagem = $"Cliente com id {id} não encontrado." });

            var possuiAlugueis = await _context.Alugueis.AnyAsync(a => a.ClienteId == id);
            if (possuiAlugueis)
                return Conflict(new { mensagem = "Não é possível excluir um cliente que possui aluguéis registrados." });

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>
        /// Retorna uma mensagem de erro caso CPF ou e-mail já estejam em uso por outro cliente, ou null se estiver tudo certo.
        /// </summary>
        private async Task<string?> ValidarCpfEmailUnicos(string cpf, string email, int? idIgnorado = null)
        {
            var cpfEmUso = await _context.Clientes.AnyAsync(c => c.CPF == cpf && c.ClienteId != idIgnorado);
            if (cpfEmUso)
                return $"Já existe um cliente cadastrado com o CPF '{cpf}'.";

            var emailEmUso = await _context.Clientes.AnyAsync(c => c.Email == email && c.ClienteId != idIgnorado);
            if (emailEmUso)
                return $"Já existe um cliente cadastrado com o e-mail '{email}'.";

            return null;
        }
    }
}
