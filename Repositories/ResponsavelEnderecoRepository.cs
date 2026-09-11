namespace Vitalis.Repositories;

public class ResponsavelEnderecoRepository : IResponsavelEnderecoRepository
{
    private readonly AppDbContext _context;

    public ResponsavelEnderecoRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<ResponsavelEndereco> GetByResponsavelId(long responsavelId)
        => _context.Enderecos
            .Where(e => e.ResponsavelId == responsavelId)
            .ToList();

    public ResponsavelEndereco? GetById(long id)
        => _context.Enderecos.FirstOrDefault(e => e.Id == id);

    public void Add(ResponsavelEndereco endereco)
    {
        bool temOutros = _context.Enderecos.Any(e => e.ResponsavelId == endereco.ResponsavelId);
        if (!temOutros)
            endereco.Principal = true;

        if (endereco.Principal)
            DesmarcarOutros(endereco.ResponsavelId, excludeId: null);

        _context.Enderecos.Add(endereco);
        _context.SaveChanges();
    }

    public void Update(ResponsavelEndereco endereco)
    {
        if (endereco.Principal)
            DesmarcarOutros(endereco.ResponsavelId, excludeId: endereco.Id);

        _context.Enderecos.Update(endereco);
        _context.SaveChanges();
    }

    public void Delete(long id)
    {
        var endereco = GetById(id);
        if (endereco == null) return;

        _context.Enderecos.Remove(endereco);
        _context.SaveChanges();

        if (endereco.Principal)
        {
            var substituto = _context.Enderecos
                .Where(e => e.ResponsavelId == endereco.ResponsavelId)
                .OrderByDescending(e => e.Id)
                .FirstOrDefault();

            if (substituto != null)
            {
                substituto.Principal = true;
                _context.SaveChanges();
            }
        }
    }

    public void SetPrincipal(long responsavelId, long enderecoId)
    {
        DesmarcarOutros(responsavelId, excludeId: enderecoId);

        var alvo = GetById(enderecoId);
        if (alvo != null)
        {
            alvo.Principal = true;
            _context.SaveChanges();
        }
    }

    private void DesmarcarOutros(long responsavelId, long? excludeId)
    {
        var outros = _context.Enderecos
            .Where(e => e.ResponsavelId == responsavelId && e.Principal &&
                        (excludeId == null || e.Id != excludeId))
            .ToList();

        foreach (var e in outros)
            e.Principal = false;

    }
}