using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vitalis.Models;
using Vitalis.Observabilidade;
using Vitalis.Repositories;

[ApiController]
[Route("api/lembretes")]
public class LembretesApiController : ControllerBase
{
    private readonly ILembreteRepository _repo;
    private readonly IConfiguration _config;
    // Logger usado para o registro estruturado das operações do controller
    private readonly ILogger<LembretesApiController> _logger;

    public LembretesApiController(
        ILembreteRepository repo,
        IConfiguration config,
        ILogger<LembretesApiController> logger)
    {
        _repo = repo;
        _config = config;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        // Grava log de informação antes de processar a consulta
        _logger.LogInformation("Buscando listagem completa de lembretes.");

        return Ok(_repo.GetAll().Select(l => new
        {
            l.Id, l.ResponsavelId, l.PetId, l.Tipo,
            l.DataAgendada, l.Mensagem, l.Status
        }));
    }

    [HttpGet("{id:long}")]
    public IActionResult GetById(long id)
    {
        // Grava log estruturado contendo o parâmetro da busca
        _logger.LogInformation("Buscando lembrete com ID: {LembreteId}", id);

        var lembrete = _repo.GetById(id);
        if (lembrete == null)
        {
            // Grava log de aviso indicando recurso não encontrado
            _logger.LogWarning("Lembrete com ID {LembreteId} não foi encontrado.", id);
            return NotFound(new { erro = "Lembrete não encontrado" });
        }

        return Ok(lembrete);
    }

    [HttpGet("responsavel/{responsavelId:long}")]
    public IActionResult GetByResponsavel(long responsavelId)
    {
        _logger.LogInformation("Buscando lembretes do responsável {ResponsavelId}.", responsavelId);

        var lembretes = _repo.GetByResponsavelId(responsavelId);
        return Ok(lembretes);
    }

    [HttpGet("responsavel/{responsavelId:long}/tipo/{tipo}")]
    public IActionResult GetByResponsavelETipo(long responsavelId, TipoLembrete tipo)
    {
        _logger.LogInformation(
            "Buscando lembretes do responsável {ResponsavelId} do tipo {TipoLembrete}.", responsavelId, tipo);

        var lembretes = _repo.GetByResponsavelIdETipo(responsavelId, tipo);
        return Ok(lembretes);
    }

    [HttpPost]
    public IActionResult Criar([FromBody] CriarLembreteDto dto)
    {
        // Inicia um Span customizado via ActivitySource para rastreamento refinado
        using var activity =
            AplicacaoMetricas.ActivitySourceAplicacao.StartActivity("CriarLembreteEndpoint");
        activity?.SetTag("lembrete.tipo", dto.Tipo.ToString());
        activity?.SetTag("lembrete.responsavelId", dto.ResponsavelId);

        var token = Request.Headers["X-Service-Token"].ToString();
        if (token != _config["ServiceToken"])
        {
            // Registra a falha de autenticação no Span e incrementa a métrica de erro
            activity?.SetStatus(ActivityStatusCode.Error, "Token inválido");
            AplicacaoMetricas.LembretesCriadosContador.Add(1,
                new KeyValuePair<string, object?>("status", "erro_token"));

            _logger.LogWarning("Criação de lembrete recusada por Service Token inválido.");
            return Unauthorized(new { erro = "Token inválido" });
        }

        if (!ModelState.IsValid)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Dados inválidos");
            AplicacaoMetricas.LembretesCriadosContador.Add(1,
                new KeyValuePair<string, object?>("status", "erro_validacao"));

            _logger.LogWarning("Criação de lembrete rejeitada por dados inválidos.");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation(
                "Tentando criar lembrete do tipo {TipoLembrete} para o responsável {ResponsavelId}.",
                dto.Tipo, dto.ResponsavelId);

            var lembrete = new Lembrete
            {
                ResponsavelId  = dto.ResponsavelId,
                PetId          = dto.PetId,
                Tipo           = dto.Tipo,
                DataAgendada   = dto.DataAgendada,
                Mensagem       = dto.Mensagem,
                ReferenciaId   = dto.ReferenciaId,
                ReferenciaTipo = dto.ReferenciaTipo,
            };

            _repo.Add(lembrete);

            // Incrementa a métrica customizada de lembretes criados
            AplicacaoMetricas.LembretesCriadosContador.Add(1,
                new KeyValuePair<string, object?>("status", "sucesso"));

            _logger.LogInformation("Lembrete {LembreteId} criado com sucesso.", lembrete.Id);

            return CreatedAtAction(nameof(GetById), new { id = lembrete.Id }, new { lembrete.Id });
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Falha inesperada na criação");
            AplicacaoMetricas.LembretesCriadosContador.Add(1,
                new KeyValuePair<string, object?>("status", "erro_interno"));

            _logger.LogError(ex,
                "Falha inesperada ao criar lembrete do tipo {TipoLembrete} para o responsável {ResponsavelId}.",
                dto.Tipo, dto.ResponsavelId);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new { erro = "Erro interno ao criar lembrete" });
        }
    }

    [HttpPatch("{id:long}/status")]
    public IActionResult AtualizarStatus(long id, [FromBody] AtualizarStatusDto dto)
    {
        var lembrete = _repo.GetById(id);
        if (lembrete == null)
        {
            _logger.LogWarning("Atualização de status falhou: lembrete {LembreteId} não encontrado.", id);
            return NotFound(new { erro = "Lembrete não encontrado" });
        }

        _repo.AtualizarStatus(id, dto.Status);

        _logger.LogInformation(
            "Status do lembrete {LembreteId} atualizado para {StatusLembrete}.", id, dto.Status);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id)
    {
        var lembrete = _repo.GetById(id);
        if (lembrete == null)
        {
            _logger.LogWarning("Remoção falhou: lembrete {LembreteId} não encontrado.", id);
            return NotFound(new { erro = "Lembrete não encontrado" });
        }

        _repo.Delete(id);

        _logger.LogInformation("Lembrete {LembreteId} removido com sucesso.", id);
        return NoContent();
    }
}
