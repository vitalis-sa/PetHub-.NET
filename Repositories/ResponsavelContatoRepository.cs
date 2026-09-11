namespace Vitalis.Repositories;

public class ResponsavelContatoRepository : IResponsavelContatoRepository
{
    private readonly AppDbContext _context;

    public ResponsavelContatoRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<ResponsavelContato> GetByResponsavelId(long responsavelId)
        => _context.Contatos
            .Where(c => c.ResponsavelId == responsavelId)
            .ToList();

    public ResponsavelContato? GetById(long id)
        => _context.Contatos.FirstOrDefault(c => c.Id == id);

    public void Add(ResponsavelContato contato)
    {
        bool temOutros = _context.Contatos.Any(c => c.ResponsavelId == contato.ResponsavelId);
        if (!temOutros)
            contato.Principal = true;

        if (contato.Principal)
            DesmarcarOutros(contato.ResponsavelId, excludeId: null);

        _context.Contatos.Add(contato);
        _context.SaveChanges();
    }

    public void Update(ResponsavelContato contato)
    {
        if (contato.Principal)
            DesmarcarOutros(contato.ResponsavelId, excludeId: contato.Id);

        _context.Contatos.Update(contato);
        _context.SaveChanges();
    }

    public void Delete(long id)
    {
        var contato = GetById(id);
        if (contato == null) return;

        _context.Contatos.Remove(contato);
        _context.SaveChanges();

        if (contato.Principal)
        {
            var substituto = _context.Contatos
                .Where(c => c.ResponsavelId == contato.ResponsavelId)
                .OrderByDescending(c => c.Id)
                .FirstOrDefault();

            if (substituto != null)
            {
                substituto.Principal = true;
                _context.SaveChanges();
            }
        }
    }

    public void SetPrincipal(long responsavelId, long contatoId)
    {
        DesmarcarOutros(responsavelId, excludeId: contatoId);

        var alvo = GetById(contatoId);
        if (alvo != null)
        {
            alvo.Principal = true;
            _context.SaveChanges();
        }
    }

    private void DesmarcarOutros(long responsavelId, long? excludeId)
    {
        var outros = _context.Contatos
            .Where(c => c.ResponsavelId == responsavelId && c.Principal &&
                        (excludeId == null || c.Id != excludeId))
            .ToList();

        foreach (var c in outros)
            c.Principal = false;
    }
}