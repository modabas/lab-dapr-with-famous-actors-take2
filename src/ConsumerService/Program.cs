using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using System.Net;

namespace ConsumerService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDaprSidekick(builder.Configuration);
        }

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Host.UseOrleans(builder =>
        {
            builder.UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "Mod.DaprWithFamousActors.Take2";
                    options.ServiceId = "Consumer";
                })
                .Configure<EndpointOptions>(options =>
                {
                    options.AdvertisedIPAddress = IPAddress.Loopback;
                    options.SiloPort = 11112;
                    options.GatewayPort = 0;
                })
                //For tracing
                .AddActivityPropagation()
                .ConfigureLogging(logging => logging.AddConsole());
        });

        //Open Telemetry Configuration
        // Define some important constants to initialize tracing with
        var serviceName = "mod-daprwithfamousactors-take2-consumer";
        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "?";

        var otelAttributes = new Dictionary<string, object>
        {
            { "deployment.environment", builder.Environment.EnvironmentName }
        };
        builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(serviceName)

                //orleans
                .AddSource("Microsoft.Orleans.Application")

                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                        .AddAttributes(otelAttributes)
                        .AddTelemetrySdk())

                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(otlpExporter =>
                {
                    otlpExporter.Protocol = OtlpExportProtocol.Grpc;
                });
        });

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                        .AddAttributes(otelAttributes)
                        .AddTelemetrySdk());     
            options.AddOtlpExporter(otlpExporter =>
            {
                otlpExporter.Protocol = OtlpExportProtocol.Grpc;
            });
        });

        var app = builder.Build();

        // Dapr will send serialized event object vs. being raw CloudEvent
        app.UseCloudEvents();

        // needed for Dapr pub/sub routing
        app.MapSubscribeHandler();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        //app.UseHttpsRedirection();

        //app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}
