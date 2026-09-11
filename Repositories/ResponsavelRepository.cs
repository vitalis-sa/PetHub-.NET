using Microsoft.EntityFrameworkCore;

namespace Vitalis.Repositories;

public class ResponsavelRepository : IResponsavelRepository
{
    private readonly AppDbContext _context;

    public ResponsavelRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Responsavel> GetAll()
        => _context.Responsavels
            .Include(t => t.Enderecos)
            .Include(t => t.Contatos)
            .ToList();

    public Responsavel? GetById(long id)
        => _context.Responsavels
            .Include(t => t.Enderecos)
            .Include(t => t.Contatos)
            .FirstOrDefault(t => t.Id == id);

    public Responsavel? GetByCpf(string cpf)
        => _context.Responsavels
            .FirstOrDefault(t => t.Cpf == cpf && t.Ativo);

    public void Add(Responsavel responsavel)
    {
        responsavel.CreatedAt = DateTime.UtcNow;
        responsavel.Senha = BCrypt.Net.BCrypt.HashPassword(responsavel.Senha);
        _context.Responsavels.Add(responsavel);
        _context.SaveChanges();
    }

    public void Update(Responsavel responsavel)
    {
        var existente = _context.Responsavels
            .AsNoTracking()
            .FirstOrDefault(t => t.Id == responsavel.Id);

        if (existente != null)
            responsavel.Senha = existente.Senha;

        _context.Responsavels.Update(responsavel);
        _context.SaveChanges();
    }

    public void Delete(long id)
    {
        var responsavel = GetById(id);
        if (responsavel != null)
        {
            _context.Responsavels.Remove(responsavel);
            _context.SaveChanges();
        }
    }
    
    public Responsavel? GetByEmail(string email)
        => _context.Responsavels
            .FirstOrDefault(t => t.Email == email);
}