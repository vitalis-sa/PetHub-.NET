using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Vitalis.Tests.Unit.Fixtures;

public class ConfiguracaoFixture
{
    public const string ServiceTokenValido = "token-de-teste-2026";

    public IConfiguration Configuration { get; } = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServiceToken"] = ServiceTokenValido
        })
        .Build();

    public static T ComHttpContext<T>(T controller, string? serviceToken = null) where T : ControllerBase
    {
        var httpContext = new DefaultHttpContext();

        if (serviceToken is not null)
            httpContext.Request.Headers["X-Service-Token"] = serviceToken;

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}
