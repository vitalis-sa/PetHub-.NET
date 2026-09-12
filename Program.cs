using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Vitalis.Health;
using Vitalis.Middlewares;
using Vitalis.Observabilidade;
using Vitalis.Repositories;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/vitalis-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleConnection")));

builder.Services.AddScoped<IResponsavelRepository,         ResponsavelRepository>();
builder.Services.AddScoped<IResponsavelEnderecoRepository, ResponsavelEnderecoRepository>();
builder.Services.AddScoped<IResponsavelContatoRepository,  ResponsavelContatoRepository>();
builder.Services.AddScoped<ILembreteRepository,            LembreteRepository>();

builder.Services.AddControllers();
builder.Services.AddControllersWithViews();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "Vitalis API",
        Version     = "v1",
        Description = "API do domínio do Responsavel — PetHub"
    });
});

builder.Services.AddHttpClient();

builder.Services.AddHealthChecks()
    .AddCheck<BancoDadosHealthCheck>("banco_dados")
    .AddCheck<ServicoExternoHealthCheck>("servico_externo");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(AplicacaoMetricas.NomeServico))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource(AplicacaoMetricas.NomeServico)
            .AddConsoleExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddMeter(AplicacaoMetricas.NomeServico)
            .AddConsoleExporter();
    });

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Vitalis API v1");
    c.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = RespostaHealthCheck.EscreverAsync
});

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

public partial class Program;
