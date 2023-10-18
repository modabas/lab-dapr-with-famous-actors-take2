using Dapr.Client;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using PublisherService.Core.Database.Config;
using PublisherService.Core.Database.OutboxPattern.OutboxSelector;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.OutboxPattern.Utility;
using PublisherService.Core.GreetService.Service;
using PublisherService.Infrastructure.Database.Postgres.EntityFw.Extensions;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;
using System.Net;
using System.Text;

namespace PublisherService;

public class Program
{
    public static void Main(string[] args)
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddDaprSidekick(builder.Configuration);
        }

        // Add outbox services
        builder.Services.Configure<ServiceDbOptions>(builder.Configuration.GetSection("ServiceDbOptions"));
        builder.Services.Configure<OutboxPatternOptions>(builder.Configuration.GetSection("OutboxPatternOptions"));
        builder.Services.AddSingleton<IOutboxPatternDbContext, OutboxPatternDbContext>();
        builder.Services.AddSingleton<IOutboxPersistor, OutboxPersistor>();
        builder.Services.AddTransient<IOutboxSelector, RandomOutboxSelector>();

        //Greet service Dapper implementation
        //builder.Services.AddSingleton<Infrastructure.Database.Postgres.Dapper.Context.IApplicationDbContext, Infrastructure.Database.Postgres.Dapper.Context.ApplicationDbContext>();
        //builder.Services.AddScoped<IGreetingService, Infrastructure.Database.Postgres.Dapper.GreetService.Service.GreetingService>();

        //Greet service Entity fw implementation
        builder.Services.RegisterApplicationDbContext(builder.Configuration);
        builder.Services.AddScoped<IGreetingService, Infrastructure.Database.Postgres.EntityFw.GreetService.Service.GreetingService>();


        builder.Services.AddSingleton<DaprClient>(new DaprClientBuilder().Build());

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
                    options.ServiceId = "Publisher";
                })
                .Configure<EndpointOptions>(options =>
                {
                   options.AdvertisedIPAddress = IPAddress.Loopback;
                   options.SiloPort = 11111;
                   options.GatewayPort = 0;
                })
                .UseInMemoryReminderService()
                .AddStartupTask<DbPartitionCreationStartupTask>()
                .ConfigureServices(conf => conf.AddSingleton<IOutboxProcessor, OutboxProcessor>())
                .AddGrainService<OutboxListenerGrainService>()
                //For tracing
                .AddActivityPropagation()
                .ConfigureLogging(logging => logging.AddConsole());
        });

        //Open Telemetry Configuration
        // Define some important constants to initialize tracing with
        var serviceName = "mod-daprwithfamousactors-take2-publisher";
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
