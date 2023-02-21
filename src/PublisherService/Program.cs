using Dapr.Client;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Orleans.Configuration;
using PublisherService.Core.Database.Config;
using PublisherService.Core.Database.OutboxPattern.Orleans;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.Service;
using PublisherService.Core.GreetService.Service;
using PublisherService.Infrastructure.Database.Postgres.Dapper.Service;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;
using System.Net;

namespace PublisherService;

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

        //Db Services
        builder.Services.Configure<ServiceDbOptions>(builder.Configuration.GetSection("ServiceDbOptions"));
        builder.Services.AddSingleton<IDbContext, DbContext>();
        builder.Services.AddSingleton<IOutboxPublisher, OutboxPublisher>();
        builder.Services.AddSingleton<IGreetingRepo, GreetingRepo>();


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

        builder.Services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddZipkinExporter()
                .AddSource(serviceName)

                //orleans
                .AddSource("Microsoft.Orleans.Application")

                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddNpgsql();
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
