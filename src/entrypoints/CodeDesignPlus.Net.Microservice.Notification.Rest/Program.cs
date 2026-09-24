using CodeDesignPlus.Net.Observability.Extensions;
using CodeDesignPlus.Net.Microservice.Commons.Application;
using CodeDesignPlus.Net.Microservice.Commons.EntryPoints.Rest.Middlewares;
using CodeDesignPlus.Net.Microservice.Commons.EntryPoints.Rest.Resources;
using CodeDesignPlus.Net.Microservice.Commons.EntryPoints.Rest.Swagger;
using CodeDesignPlus.Net.Microservice.Commons.FluentValidation;
using CodeDesignPlus.Net.Microservice.Commons.HealthChecks;
using CodeDesignPlus.Net.Microservice.Commons.MediatR;
using CodeDesignPlus.Net.Redis.Cache.Extensions;
using CodeDesignPlus.Net.Vault.Extensions;
using NodaTime.Serialization.JsonNet;

var builder = WebApplication.CreateSlimBuilder(args);

Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.Host.UseSerilog();

builder.Configuration.AddVault();

builder.Services
    .AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Error;
        options.SerializerSettings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors();
builder.Services.AddVault(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddMongo<CodeDesignPlus.Net.Microservice.Notification.Infrastructure.Startup>(builder.Configuration);
builder.Services.AddObservability(builder.Configuration, builder.Environment);
builder.Services.AddLogger(builder.Configuration);
builder.Services.AddRabbitMQ<CodeDesignPlus.Net.Microservice.Notification.Domain.Startup>(builder.Configuration);
builder.Services.AddServiceBus<CodeDesignPlus.Net.Microservice.Notification.Domain.Startup>(builder.Configuration);
builder.Services.AddMapster();
builder.Services.AddFluentValidation();
builder.Services.AddMediatR<CodeDesignPlus.Net.Microservice.Notification.Application.Startup>();
builder.Services.AddSecurity(builder.Configuration);

// La bandeja resuelve los roles del lector contra su copropiedad con IRoleDirectory, y el ultimo recurso
// de ese directorio -preguntarle a ms-users- vive en los clientes gRPC. Sin esta linea el directorio se
// queda sin ninguna fuente y devuelve vacio, que deniega: la bandeja sale en blanco para todo el mundo y
// no falla nada. Estaba solo en el entrypoint gRPC, y quien lee la bandeja es este.
builder.Services.AddGrpcClients(builder.Configuration);
builder.Services.AddCoreSwagger<Program>(builder.Configuration);
builder.Services.AddCache(builder.Configuration);
builder.Services.AddResources<Program>(builder.Configuration);
builder.Services.AddHealthChecksServices();

var app = builder.Build();

app.UseCors(builder => builder
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader()
);

app.UseTraceContext();

app.UsePath();

app.UseLanguageMiddleware();
app.UseExceptionMiddleware();
app.UseHealthChecks();
app.UseCodeErrors();
app.UseCodeErrorsValidation();

app.UseCoreSwagger();

app.UseHttpsRedirection();

app.UseAuth();

app.MapControllers().RequireAuthorization();

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
