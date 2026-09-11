using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Vitalis.Tests.Integration.Fixtures;

public class VitalisWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string ServiceToken = "token-de-integracao-2026";

    private readonly string _nomeDoBanco = $"vitalis-integracao-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ServiceToken", ServiceToken);
        builder.UseSetting("ServicosExternos:PethubJava", "http://127.0.0.1:9/health");

        builder.ConfigureServices(services =>
        {
            RemoverRegistroDoOracle(services);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_nomeDoBanco));
        });
    }

    private static void RemoverRegistroDoOracle(IServiceCollection services)
    {
        var registros = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(AppDbContext) ||
                descriptor.ServiceType == typeof(DbContextOptions) ||
                descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                EhConfiguracaoDeOpcoesDoContexto(descriptor.ServiceType))
            .ToList();

        foreach (var registro in registros)
            services.Remove(registro);
    }

    private static bool EhConfiguracaoDeOpcoesDoContexto(Type serviceType)
        => serviceType.IsGenericType
           && serviceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1"
           && serviceType.GenericTypeArguments[0] == typeof(AppDbContext);

    public void LimparBanco()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Lembretes.RemoveRange(context.Lembretes);
        context.Contatos.RemoveRange(context.Contatos);
        context.Enderecos.RemoveRange(context.Enderecos);
        context.Responsavels.RemoveRange(context.Responsavels);
        context.SaveChanges();
        context.ChangeTracker.Clear();
    }

    public HttpClient CreateClientComServiceToken(string token = ServiceToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Service-Token", token);
        return client;
    }
}

[CollectionDefinition(Name)]
public class VitalisApiCollection : ICollectionFixture<VitalisWebApplicationFactory>
{
    public const string Name = "API Vitalis em memória";
}
