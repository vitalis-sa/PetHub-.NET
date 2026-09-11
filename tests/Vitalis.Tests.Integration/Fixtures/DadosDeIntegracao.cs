using System.Net.Http.Json;
using System.Text.Json;
using Vitalis.Models;

namespace Vitalis.Tests.Integration.Fixtures;

public static class DadosDeIntegracao
{
    public const string SenhaPadrao = "SenhaSegura@123";

    public static object NovoCadastro(
        string nome = "Pedro Chasci",
        string cpf = "12345678901",
        string email = "pedro@pethub.com")
        => new { nome, cpf, email, senha = SenhaPadrao };

    public static object NovoLogin(string email = "pedro@pethub.com", string senha = SenhaPadrao)
        => new { email, senha };

    public static object NovoEndereco()
        => new
        {
            logradouro = "Av. Paulista",
            numero = "1000",
            complemento = "Sala 42",
            bairro = "Bela Vista",
            cidade = "São Paulo",
            estado = "SP",
            cep = "01310100",
            principal = false
        };

    public static object NovoContato(string telefone = "11999998888")
        => new { tipo = "CELULAR", telefone, principal = false };

    public static object NovoLembrete(long responsavelId, TipoLembrete tipo = TipoLembrete.VACINA)
        => new
        {
            responsavelId,
            petId = 7,
            tipo = (int)tipo,
            dataAgendada = "2026-09-15",
            mensagem = "Vacina antirrábica agendada",
            referenciaId = 99,
            referenciaTipo = "VACINA"
        };

    public static async Task<long> CadastrarResponsavelAsync(
        HttpClient client, string cpf = "12345678901", string email = "pedro@pethub.com")
    {
        var resposta = await client.PostAsJsonAsync("/api/responsavel/cadastro", NovoCadastro(cpf: cpf, email: email));
        resposta.EnsureSuccessStatusCode();

        var criado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        return criado.GetProperty("id").GetInt64();
    }
}
