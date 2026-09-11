namespace Vitalis.Repositories;

public interface IResponsavelRepository
{
    IEnumerable<Responsavel> GetAll();
    Responsavel? GetById(long id);
    Responsavel? GetByCpf(string cpf);
    void Add(Responsavel responsavel);
    void Update(Responsavel responsavel);
    void Delete(long id);
    Responsavel? GetByEmail(string email);
}