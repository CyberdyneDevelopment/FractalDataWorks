using System;
using Fdw.Services.Messaging;
using Shouldly;
using Xunit;

namespace Fdw.Services.Messaging.Tests;

/// <summary>
/// Tests for the times of a <see cref="MessagePayload"/> (FDW-2918): a time read back from a datetime2 column has no Kind and would be
/// serialised without a zone, so the payload gives every time its zone.
/// </summary>
public sealed class MessagePayloadTests
{
    private static readonly DateTime Moment = new(2026, 10, 27, 17, 9, 27, DateTimeKind.Unspecified);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATimeReadWithNoKindIsUtc()
    {
        var payload = new MessagePayload { CreatedAt = Moment, ReadAt = Moment, DeliveredAt = Moment, DismissedAt = Moment, ArchivedAt = Moment };

        payload.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        payload.CreatedAt.ShouldBe(new DateTime(2026, 10, 27, 17, 9, 27, DateTimeKind.Utc));
        payload.ReadAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        payload.DeliveredAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        payload.DismissedAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        payload.ArchivedAt!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ALocalTimeIsConvertedNotReinterpreted()
    {
        var local = new DateTime(2026, 10, 27, 17, 9, 27, DateTimeKind.Local);

        var payload = new MessagePayload { CreatedAt = local };

        payload.CreatedAt.ShouldBe(local.ToUniversalTime());
        payload.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATimeThatWasNeverSetStaysNone()
    {
        var payload = new MessagePayload();

        payload.ReadAt.ShouldBeNull();
        payload.ArchivedAt.ShouldBeNull();
    }
}
