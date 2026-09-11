using Vitalis.Models;

namespace Vitalis.Repositories;

public interface ILembreteRepository
{
    IEnumerable<Lembrete> GetAll();
    IEnumerable<Lembrete> GetByResponsavelId(long responsavelId);
    IEnumerable<Lembrete> GetByResponsavelIdETipo(long responsavelId, TipoLembrete tipo);
    Lembrete? GetById(long id);
    void Add(Lembrete lembrete);
    void AtualizarStatus(long id, StatusLembrete status);
    void Delete(long id);
}