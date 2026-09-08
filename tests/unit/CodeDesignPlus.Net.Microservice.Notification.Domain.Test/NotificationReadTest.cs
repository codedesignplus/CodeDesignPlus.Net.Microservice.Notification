namespace CodeDesignPlus.Net.Microservice.Notification.Domain.Test;

/// <summary>
/// Cubre el acuse de que alguien leyo un aviso.
/// </summary>
/// <remarks>
/// Vive en coleccion aparte y no como array dentro del aviso: uno dirigido a toda la copropiedad lo
/// leerian todos sus usuarios, y ese array no tendria techo. Un documento que crece sin limite acaba
/// pasandose del tamaño maximo de Mongo, y lo hace el dia que el conjunto es grande.
/// </remarks>
public class NotificationReadTest
{
    private static readonly Guid AvisoId = Guid.Parse("aa000000-0000-4000-8000-000000000001");
    private static readonly Guid OtroAviso = Guid.Parse("bb000000-0000-4000-8000-000000000002");
    private static readonly Guid Yo = Guid.Parse("cc000000-0000-4000-8000-000000000003");
    private static readonly Guid Vecino = Guid.Parse("dd000000-0000-4000-8000-000000000004");
    private static readonly Guid Tenant = Guid.Parse("ee000000-0000-4000-8000-000000000005");

    [Fact]
    public void TheReadReceiptIdIsDeterministicSoReadingTwiceDoesNotDuplicate()
    {
        // Marcar leido es idempotente por construccion: el id sale del par (aviso, lector), asi que un
        // doble clic o la misma pantalla en dos pestañas actualizan la misma fila en vez de acumular dos.
        var primero = NotificationReadAggregate.Create(AvisoId, Yo, Tenant);
        var segundo = NotificationReadAggregate.Create(AvisoId, Yo, Tenant);

        Assert.Equal(primero.Id, segundo.Id);
    }

    [Fact]
    public void TwoUsersReadingTheSameNoticeGetDifferentReceipts()
    {
        // Si el id solo dependiera del aviso, que un vecino lo lea lo marcaria como leido para todos.
        var mio = NotificationReadAggregate.Create(AvisoId, Yo, Tenant);
        var suyo = NotificationReadAggregate.Create(AvisoId, Vecino, Tenant);

        Assert.NotEqual(mio.Id, suyo.Id);
    }

    [Fact]
    public void TheSameReaderOnTwoNoticesGetsTwoReceipts()
    {
        // Y al reves: si el id solo dependiera del lector, leer uno los marcaria todos.
        var uno = NotificationReadAggregate.Create(AvisoId, Yo, Tenant);
        var otro = NotificationReadAggregate.Create(OtroAviso, Yo, Tenant);

        Assert.NotEqual(uno.Id, otro.Id);
    }

    [Fact]
    public void TheReceiptKeepsWhoReadWhatAndWhen()
    {
        var acuse = NotificationReadAggregate.Create(AvisoId, Yo, Tenant);

        Assert.Equal(AvisoId, acuse.NotificationId);
        Assert.Equal(Yo, acuse.UserId);
        Assert.Equal(Tenant, acuse.Tenant);
        Assert.NotEqual(default, acuse.ReadAt);
    }

    [Fact]
    public void AReceiptWithoutANoticeIsRejected()
        => Assert.Throws<CodeDesignPlusException>(() => NotificationReadAggregate.Create(Guid.Empty, Yo, Tenant));

    [Fact]
    public void AReceiptWithoutAReaderIsRejected()
        => Assert.Throws<CodeDesignPlusException>(() => NotificationReadAggregate.Create(AvisoId, Guid.Empty, Tenant));

    [Fact]
    public void TheIdIsStableAcrossRuns()
    {
        // El valor esta escrito a mano a proposito: si el algoritmo cambiara, todos los acuses ya
        // guardados dejarian de encontrarse y la bandeja volveria a marcar como no leido lo que si se
        // leyo. Una prueba que recalcula el valor con el mismo codigo no detectaria eso.
        var acuse = NotificationReadAggregate.Create(AvisoId, Yo, Tenant);

        Assert.Equal(Guid.Parse("99b6ed21-6d54-57db-9b95-a9baf01f940e"), acuse.Id);
    }
}
