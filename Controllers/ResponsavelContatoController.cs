using Microsoft.AspNetCore.Mvc;
using Vitalis.Repositories;

[ApiController]
[Route("api/responsavel/{responsavelId:long}/contatos")]
public class ResponsavelContatoController : ControllerBase
{
    private readonly IResponsavelContatoRepository _repo;
    // Logger usado para o registro estruturado das operações do controller
    private readonly ILogger<ResponsavelContatoController> _logger;

    public ResponsavelContatoController(
        IResponsavelContatoRepository repo,
        ILogger<ResponsavelContatoController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetAll(long responsavelId)
    {
        // Grava log estruturado contendo o responsável consultado
        _logger.LogInformation("Buscando contatos do responsável {ResponsavelId}.", responsavelId);

        return Ok(_repo.GetByResponsavelId(responsavelId));
    }

    [HttpGet("{id:long}")]
    public IActionResult GetById(long responsavelId, long id)
    {
        var contato = _repo.GetById(id);
        if (contato == null || contato.ResponsavelId != responsavelId)
        {
            // Grava log de aviso indicando recurso não encontrado
            _logger.LogWarning("Contato {ContatoId} não encontrado para o responsável {ResponsavelId}.",
                id, responsavelId);
            return NotFound(new { erro = "Contato não encontrado" });
        }

        return Ok(contato);
    }

    [HttpPost]
    public IActionResult Add(long responsavelId, [FromBody] ResponsavelContato contato)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        contato.ResponsavelId = responsavelId;
        _repo.Add(contato);

        _logger.LogInformation("Contato {ContatoId} cadastrado para o responsável {ResponsavelId}.",
            contato.Id, responsavelId);

        return CreatedAtAction(nameof(GetById),
            new { responsavelId, id = contato.Id }, contato);
    }

    [HttpPut("{id:long}")]
    public IActionResult Update(long responsavelid, long id, [FromBody] ResponsavelContato contato)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existente = _repo.GetById(id);
        if (existente == null || existente.ResponsavelId != responsavelid)
            return NotFound(new { erro = "Contato não encontrado" });

        contato.Id      = id;
        contato.ResponsavelId = responsavelid;
        _repo.Update(contato);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public IActionResult Delete(long responsavelId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.ResponsavelId != responsavelId)
            return NotFound(new { erro = "Contato não encontrado" });

        _repo.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:long}/principal")]
    public IActionResult SetPrincipal(long responsavelId, long id)
    {
        var existente = _repo.GetById(id);
        if (existente == null || existente.ResponsavelId != responsavelId)
            return NotFound(new { erro = "Contato não encontrado" });

        _repo.SetPrincipal(responsavelId, id);
        return NoContent();
    }
}