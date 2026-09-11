using Microsoft.EntityFrameworkCore;
using Vitalis.Models;

namespace Vitalis.Repositories;

public class LembreteRepository : ILembreteRepository
{
    private readonly AppDbContext _context;

    public LembreteRepository(AppDbContext context)
    {
        _context = context;
    }

    public IEnumerable<Lembrete> GetAll()
        => _context.Lembretes
            .Include(l => l.Responsavel)
            .OrderByDescending(l => l.DataAgendada)
            .ToList();

    public IEnumerable<Lembrete> GetByResponsavelId(long responsavelId)
        => _context.Lembretes
            .Where(l => l.ResponsavelId == responsavelId)
            .OrderByDescending(l => l.DataAgendada)
            .ToList();

    public IEnumerable<Lembrete> GetByResponsavelIdETipo(long responsavelId, TipoLembrete tipo)
        => _context.Lembretes
            .Where(l => l.ResponsavelId == responsavelId && l.Tipo == tipo)
            .OrderByDescending(l => l.DataAgendada)
            .ToList();

    public Lembrete? GetById(long id)
        => _context.Lembretes
            .Include(l => l.Responsavel)
            .FirstOrDefault(l => l.Id == id);

    public void Add(Lembrete lembrete)
    {
        lembrete.CreatedAt = DateTime.UtcNow;
        lembrete.Status = StatusLembrete.PENDENTE;
        _context.Lembretes.Add(lembrete);
        _context.SaveChanges();
    }

    public void AtualizarStatus(long id, StatusLembrete status)
    {
        var lembrete = _context.Lembretes.Find(id);
        if (lembrete != null)
        {
            lembrete.Status = status;
            _context.SaveChanges();
        }
    }
    
    public void Delete(long id)
    {
        var lembrete = _context.Lembretes.Find(id);
        if (lembrete != null)
        {
            _context.Lembretes.Remove(lembrete);
            _context.SaveChanges();
        }
    }
}