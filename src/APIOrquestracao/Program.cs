using APIOrquestracao.Clients;
using APIOrquestracao.Tracing;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Sinks.Grafana.Loki;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Documentacao do OpenTelemetry:
// https://opentelemetry.io/docs/instrumentation/net/getting-started/

// Integracao do OpenTelemetry com Jaeger em .NET:
// https://github.com/open-telemetry/opentelemetry-dotnet/tree/e330e57b04fa3e51fe5d63b52bfff891fb5b7961/docs/trace/getting-started-jaeger#collect-and-visualize-traces-using-jaeger

// Documentacaoo do Jaeger:
// https://www.jaegertracing.io/docs/1.58/

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ContagemClient>();

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
                Value = "false"
            }
        })
    .CreateLogger());

builder.Services.AddOpenTelemetry().WithTracing(traceProvider =>
{
    traceProvider
        .AddSource(OpenTelemetryExtensions.ServiceName)
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(serviceName: OpenTelemetryExtensions.ServiceName,
                    serviceVersion: OpenTelemetryExtensions.ServiceVersion))
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter()
        .AddConsoleExporter();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.UseSerilogRequestLogging();

app.MapControllers();

app.Run();