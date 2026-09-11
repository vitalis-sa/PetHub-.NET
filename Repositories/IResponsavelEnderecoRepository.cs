
namespace Vitalis.Repositories;

public interface IResponsavelEnderecoRepository
{
    IEnumerable<ResponsavelEndereco> GetByResponsavelId(long responsavelId);
    ResponsavelEndereco? GetById(long id);
    void Add(ResponsavelEndereco endereco);
    void Update(ResponsavelEndereco endereco);
    void Delete(long id);
    void SetPrincipal(long responsavelId, long enderecoId);
}