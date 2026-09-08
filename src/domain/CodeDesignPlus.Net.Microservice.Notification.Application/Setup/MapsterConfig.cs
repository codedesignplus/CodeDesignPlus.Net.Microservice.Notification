using CodeDesignPlus.Net.Microservice.Notification.Domain.ValueObjects;

namespace CodeDesignPlus.Net.Microservice.Notification.Application.Setup;

public static class MapsterConfigNotifications
{
    /// <summary>
    /// Registra los mapeos de identidad de los objetos de valor del dominio.
    /// </summary>
    /// <remarks>
    /// Sin esto, exponer un VO con constructor privado en un DTO revienta con "No default constructor",
    /// y lo hace <b>el dia del primer aviso</b> —no al desplegar—, porque con la coleccion vacia el
    /// mapeo nunca llega a ejecutarse (regla 24).
    /// </remarks>
    public static void Configure()
    {
        TypeAdapterConfig<Audience, Audience>.NewConfig().MapWith(src => src);
        TypeAdapterConfig<ResourceRef, ResourceRef>.NewConfig().MapWith(src => src);
    }
}
