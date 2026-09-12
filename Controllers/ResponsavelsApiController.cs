using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Observabilidade;
using Vitalis.Repositories;

[ApiController]
[Route("api/responsavel")]
public class ResponsavelsApiController : ControllerBase
{
    private readonly IResponsavelRepository _repo;
    private readonly IConfiguration _config;
    // Logger usado para o registro estruturado das operações do controller
    private readonly ILogger<ResponsavelsApiController> _logger;

    public ResponsavelsApiController(
        IResponsavelRepository repo,
        IConfiguration config,
        ILogger<ResponsavelsApiController> logger)
    {
        _repo = repo;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        // Grava log de informação antes de processar a consulta
        _logger.LogInformation("Buscando listagem completa de responsáveis.");

        var responsavels = _repo.GetAll().Select(t => new
        {
            t.Id, t.Nome, t.Email, t.Cpf, t.Ativo, t.CreatedAt
        });
        return Ok(responsavels);
    }

    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        // Grava log estruturado contendo o parâmetro da busca
        _logger.LogInformation("Buscando responsável com ID: {ResponsavelId}", id);

        var responsavel = _repo.GetById(id);
        if (responsavel == null)
        {
            // Grava log de aviso indicando recurso não encontrado
            _logger.LogWarning("Responsável com ID {ResponsavelId} não foi encontrado.", id);
            return NotFound(new { erro = "Responsavel não encontrado" });
        }

        return Ok(new
        {
            responsavel.Id, responsavel.Nome, responsavel.Email, responsavel.Cpf,
            responsavel.Ativo, responsavel.CreatedAt,
            responsavel.Enderecos, responsavel.Contatos
        });
    }

    [HttpGet("buscar")]
    public IActionResult BuscarPorCpf([FromQuery] string cpf)
    {
        if (!ValidarServiceToken())
        {
            // Grava log de aviso indicando tentativa de acesso não autorizada
            _logger.LogWarning("Tentativa de busca por CPF com Service Token inválido.");
            return Unauthorized(new { erro = "Token inválido" });
        }

        if (string.IsNullOrWhiteSpace(cpf))
            return BadRequest(new { erro = "CPF é obrigatório" });

        _logger.LogInformation("Buscando responsável por CPF para integração com o backend Java.");

        var responsavel = _repo.GetByCpf(cpf);
        if (responsavel == null)
        {
            _logger.LogWarning("Nenhum responsável encontrado para o CPF informado.");
            return NotFound(new { erro = "Responsavel não encontrado" });
        }

        return Ok(new { responsavel.Id, responsavel.Nome, responsavel.Cpf, responsavel.Email, responsavel.Ativo });
    }

    [HttpPost("cadastro")]
    public IActionResult Cadastrar([FromBody] CadastrarResponsavelDto dto)
    {
        // Inicia um Span customizado via ActivitySource para rastreamento refinado
        using var activity =
            AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("CadastrarResponsavelEndpoint");
        activity?.SetTag("responsavel.nome", dto.Nome);

        if (!ModelState.IsValid)
        {
            // Registra a falha de validação no Span e incrementa a métrica de erro
            activity?.SetStatus(ActivityStatusCode.Error, "Dados inválidos");
            AplicacaoMetricas.ResponsaveisCadastradosContador.Add(1,
                new KeyValuePair<string, object?>("status", "erro_validacao"));

            _logger.LogWarning("Cadastro de responsável rejeitado por dados inválidos.");
            return BadRequest(ModelState);
        }

        try
        {
            if (_repo.GetByCpf(dto.Cpf) != null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "CPF já cadastrado");
                AplicacaoMetricas.ResponsaveisCadastradosContador.Add(1,
                    new KeyValuePair<string, object?>("status", "erro_cpf_duplicado"));

                _logger.LogWarning("Tentativa de cadastrar responsável com CPF já existente.");
                return Conflict(new { erro = "CPF já cadastrado" });
            }

            _logger.LogInformation("Tentando cadastrar responsável: {NomeResponsavel}", dto.Nome);

            var responsavel = new Responsavel
            {
                Nome  = dto.Nome,
                Cpf   = dto.Cpf,
                Email = dto.Email,
                Senha = dto.Senha,
                Ativo = true
            };

            _repo.Add(responsavel);

            // Incrementa a métrica customizada de responsáveis cadastrados
            AplicacaoMetricas.ResponsaveisCadastradosContador.Add(1,
                new KeyValuePair<string, object?>("status", "sucesso"));

            _logger.LogInformation("Responsável {ResponsavelId} cadastrado com sucesso.", responsavel.Id);

            return CreatedAtAction(nameof(GetById), new { id = responsavel.Id },
                new { responsavel.Id, responsavel.Nome, responsavel.Email });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Falha inesperada no cadastro");
            AplicacaoMetricas.ResponsaveisCadastradosContador.Add(1,
                new KeyValuePair<string, object?>("status", "erro_interno"));

            _logger.LogError(ex,
                "Falha inesperada ao cadastrar o responsável {NomeResponsavel}.", dto.Nome);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new { erro = "Erro interno ao cadastrar responsável" });
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Tentativa de login recebida.");

        var responsavel = _repo.GetByEmail(dto.Email);
        if (responsavel == null || !BCrypt.Net.BCrypt.Verify(dto.Senha, responsavel.Senha))
        {
            _logger.LogWarning("Login recusado por credenciais inválidas.");
            return Unauthorized(new { erro = "Credenciais inválidas" });
        }

        if (!responsavel.Ativo)
        {
            _logger.LogWarning("Login recusado: conta {ResponsavelId} está desativada.", responsavel.Id);
            return Unauthorized(new { erro = "Conta desativada" });
        }

        _logger.LogInformation("Responsável {ResponsavelId} autenticado com sucesso.", responsavel.Id);
        return Ok(new { responsavel.Id, responsavel.Nome, responsavel.Email });
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long id, [FromBody] CadastrarResponsavelDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null)
        {
            _logger.LogWarning("Atualização falhou: responsável {ResponsavelId} não encontrado.", id);
            return NotFound(new { erro = "Responsavel não encontrado" });
        }

        existente.Nome  = dto.Nome;
        existente.Email = dto.Email;
        existente.Cpf   = dto.Cpf;

        _repo.Update(existente);

        _logger.LogInformation("Responsável {ResponsavelId} atualizado com sucesso.", id);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        var responsavel = _repo.GetById(id);
        if (responsavel == null)
        {
            _logger.LogWarning("Remoção falhou: responsável {ResponsavelId} não encontrado.", id);
            return NotFound(new { erro = "Responsavel não encontrado" });
        }

        _repo.Delete(id);

        _logger.LogInformation("Responsável {ResponsavelId} removido com sucesso.", id);
        return NoContent();
    }

    private bool ValidarServiceToken()
    {
        var token = Request.Headers["X-Service-Token"].ToString();
        return token == _config["ServiceToken"];
    }
}
