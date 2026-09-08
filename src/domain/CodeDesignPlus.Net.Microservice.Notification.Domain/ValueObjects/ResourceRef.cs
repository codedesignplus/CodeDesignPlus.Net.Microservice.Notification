using Newtonsoft.Json;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

/// <summary>
/// El recurso al que lleva el clic en la bandeja.
/// </summary>
/// <remarks>
/// Un aviso **no tiene pantalla propia**: lleva al modulo dueno del dato. Una pantalla de detalle de la
/// notificacion repetiria peor lo que ese modulo ya muestra, y se desactualizaria sola.
/// <para>
/// Se guarda el modulo y el identificador, nunca una ruta: las rutas son del frontend y cambian sin que
/// el backend se entere.
/// </para>
/// </remarks>
public class ResourceRef
{
    /// <summary>Modulo dueno del dato, ej. <c>invoicing</c> o <c>communitylife</c>.</summary>
    public string Module { get; private set; }

    /// <summary>El identificador del agregado dentro de ese modulo.</summary>
    public string AggregateId { get; private set; }

    [JsonConstructor]
    private ResourceRef(string module, string aggregateId)
    {
        Module = module;
        AggregateId = aggregateId;
    }

    /// <summary>Crea la referencia al recurso que origino el aviso.</summary>
    public static ResourceRef Create(string module, string aggregateId) => new(module, aggregateId);
}
