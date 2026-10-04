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

        /// <summary>Lista todos os clientes cadastrados.</summary>
        /// <response code="200">Lista de clientes retornada com sucesso.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
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

        /// <summary>Busca um cliente pelo id.</summary>
        /// <param name="id">Id do cliente.</param>
        /// <response code="200">Cliente encontrado.</response>
        /// <response code="404">Nenhum cliente com esse id.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>Cadastra um novo cliente.</summary>
        /// <param name="dto">Nome, CPF (11 dígitos, único) e e-mail (único) são obrigatórios; telefone é opcional.</param>
        /// <response code="201">Cliente criado com sucesso.</response>
        /// <response code="400">Dados inválidos (ex.: CPF fora do formato, e-mail inválido).</response>
        /// <response code="409">Já existe um cliente com esse CPF ou e-mail.</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>Atualiza os dados de um cliente existente.</summary>
        /// <param name="id">Id do cliente a atualizar.</param>
        /// <param name="dto">Novos dados do cliente.</param>
        /// <response code="204">Atualizado com sucesso.</response>
        /// <response code="400">Dados inválidos.</response>
        /// <response code="404">Cliente não encontrado.</response>
        /// <response code="409">CPF ou e-mail já usados por outro cliente.</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        /// <summary>Exclui um cliente (somente se não houver aluguéis vinculados a ele).</summary>
        /// <param name="id">Id do cliente a excluir.</param>
        /// <response code="204">Excluído com sucesso.</response>
        /// <response code="404">Cliente não encontrado.</response>
        /// <response code="409">Existem aluguéis registrados para esse cliente.</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
