using System.Security.Cryptography;
using System.Text;

namespace CodeDesignPlus.Net.Microservice.Notification.Domain;

/// <summary>
/// Deriva un identificador estable a partir de una cadena.
/// </summary>
/// <remarks>
/// Un UUID version 5: SHA-1 de la cadena, los primeros 16 bytes, con los bits de version y variante
/// puestos. Lo que importa no es el algoritmo sino la propiedad: <b>la misma entrada da siempre el mismo
/// identificador</b>, asi que una operacion que se repite actualiza la misma fila en vez de acumular
/// duplicados.
/// <para>
/// SHA-1 se usa aqui como funcion de derivacion, no como garantia de seguridad: nadie firma ni autentica
/// nada con esto.
/// </para>
/// </remarks>
internal static class DeterministicGuid
{
    /// <summary>El identificador estable que corresponde a esa cadena.</summary>
    /// <param name="value">La clave natural, ya compuesta por quien llama.</param>
    internal static Guid From(string value)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(value));

        var bytes = new byte[16];
        Array.Copy(hash, bytes, 16);

        // Version 5 en el nibble alto del septimo byte.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        // Variante RFC 4122 en los dos bits altos del noveno.
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        // Los tres primeros grupos del Guid son little-endian en .NET; sin invertirlos el mismo hash
        // daria un texto distinto segun la plataforma.
        Array.Reverse(bytes, 0, 4);
        Array.Reverse(bytes, 4, 2);
        Array.Reverse(bytes, 6, 2);

        return new Guid(bytes);
    }
}
