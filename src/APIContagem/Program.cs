using APIContagem.Data;
using APIContagem.Tracing;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Npgsql;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using System.Reflection;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

var builder = WebApplication.CreateBuilder(args);

// Documentacao do OpenTelemetry:
// https://opentelemetry.io/docs/instrumentation/net/getting-started/

// Integracao do OpenTelemetry com Jaeger em .NET:
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/e330e57b04fa3e51fe5d63b52bfff891fb5b7961/docs/trace/getting-started-jaeger#collect-and-visualize-traces-using-jaeger

// Documentacaoo do Jaeger:
// https://www.jaegertracing.io/docs/1.58/


builder.Services.AddScoped<ContagemRepository>();

builder.Services.AddSerilog(new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.GrafanaLoki(
        builder.Configuration["Loki:Uri"]!,
        new List<LokiLabel>()
        {
            new()
            {
                Key = "Service",
                Value = System.IO.Path.GetFileName(Assembly.GetExecutingAssembly().Location).Replace(".dll", "")
            },
            new()
            {
                Key = "UsingDatabase",
                Value = "true"
            }
        })
    .CreateLogger());

builder.Services.AddDbContext<ContagemContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("BaseContagem"));
});

builder.Services.AddOpenTelemetry().WithTracing((traceBuilder) =>
{
    traceBuilder
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .AddAspNetCoreInstrumentation()
        .AddNpgsql()
        .AddOtlpExporter()
        .AddConsoleExporter();
});


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();