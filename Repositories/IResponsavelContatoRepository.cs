namespace Vitalis.Repositories;

public interface IResponsavelContatoRepository
{
    IEnumerable<ResponsavelContato> GetByResponsavelId(long responsavelId);
    ResponsavelContato? GetById(long id);
    void Add(ResponsavelContato contato);
    void Update(ResponsavelContato contato);
    void Delete(long id);
    void SetPrincipal(long responsavelId, long contatoId);
}