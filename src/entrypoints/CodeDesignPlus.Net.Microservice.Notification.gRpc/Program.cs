using CodeDesignPlus.Net.Observability.Interceptors;
using CodeDesignPlus.Net.Logger.Extensions;
using CodeDesignPlus.Net.Microservice.Commons.EntryPoints.gRpc.Interceptors;
using CodeDesignPlus.Net.Microservice.Commons.FluentValidation;
using CodeDesignPlus.Net.Microservice.Commons.HealthChecks;
using CodeDesignPlus.Net.Microservice.Commons.MediatR;
using CodeDesignPlus.Net.Microservice.Notification.Domain.Services;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Hubs;
using CodeDesignPlus.Net.Microservice.Notification.gRpc.Services;
using CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Services;
using CodeDesignPlus.Net.Mongo.Extensions;
using CodeDesignPlus.Net.Observability.Extensions;
using CodeDesignPlus.Net.RabbitMQ.Extensions;
using CodeDesignPlus.Net.Redis.Abstractions;
using CodeDesignPlus.Net.Redis.Cache.Extensions;
using CodeDesignPlus.Net.Redis.Extensions;
using CodeDesignPlus.Net.Security.Extensions;
using CodeDesignPlus.Net.Vault.Extensions;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using CodeDesignPlus.Net.gRpc.Clients.Extensions;
using CodeDesignPlus.Net.Hangfire.Extensions;

var builder = WebApplication.CreateSlimBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.Host.UseSerilog();

builder.Configuration.AddVault();

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ErrorInterceptor>();
    options.Interceptors.Add<TraceContextInterceptor>();
});
builder.Services.AddGrpcReflection();

builder.Services.AddVault(builder.Configuration);
builder.Services.AddMapster();
builder.Services.AddMediatR<CodeDesignPlus.Net.Microservice.Notification.Application.Startup>();
builder.Services.AddFluentValidation();

builder.Services.AddMongo<CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Startup>(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHangfire<Program>(builder.Configuration);
builder.Services.AddRabbitMQ<CodeDesignPlus.Net.Microservice.Notification.Domain.Startup>(builder.Configuration);
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, builder.Environment);
builder.Services.AddGrpcClients(builder.Configuration);
builder.Services.AddLogger(builder.Configuration);
builder.Services.AddCache(builder.Configuration);
builder.Services.AddHealthChecksServices();

builder.Services.AddSingleton<INotifierGateway, SignalRNotifierAdapter<MainHub>>();
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
builder.Services.AddScoped<INotificationDeliveryService, SignalRNotificationDeliveryService>();

// SignalR con Redis Backplane usando IRedisFactory del SDK CodeDesignPlus.Net.Redis.
// El ConnectionFactory se resuelve post-build capturando app.Services,
// lo que garantiza que IRedisFactory ya fue inicializado con la configuracion de Vault.
IServiceProvider? appServices = null;

builder.Services.AddSignalR()
    .AddStackExchangeRedis(o =>
    {
        o.Configuration.ChannelPrefix = RedisChannel.Literal("Notify_");
        o.ConnectionFactory = _ =>
        {
            var factory = appServices!.GetRequiredService<IRedisFactory>();
            var redis = factory.Create(FactoryConst.RedisCore);

            return Task.FromResult(redis.Connection);
        };
    });

var app = builder.Build();

// Asignar el IServiceProvider del app construido para que el ConnectionFactory lo use.
appServices = app.Services;

app.UseHealthChecks();

app.UseHangfireDashboard<Program>(app.Configuration);

app.UseAuth();

app.MapGrpcService<NotificationsService>();
app.MapHub<MainHub>("/hubs/notifications");

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
