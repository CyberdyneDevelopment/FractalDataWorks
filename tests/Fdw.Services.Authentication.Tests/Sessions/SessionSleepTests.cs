using System;
using Fdw.Services.Authentication.Sessions;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Sessions;

/// <summary>
/// When a session can be used and when it can be woken (FDW-2283): a session put to sleep by a session above it is refused exactly as a revoked one is.
/// </summary>
public sealed class SessionSleepTests
{
    private const string Service = "Console";
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static SessionEntity Live() => new()
    {
        Id = Guid.CreateVersion7(),
        ServiceName = Service,
        IdleTimeoutSeconds = 1800,
        CreatedAt = Now.AddMinutes(-10),
        LastSeenAt = Now.AddMinutes(-1),
        IdleExpiresAt = Now.AddMinutes(29),
        AbsoluteExpiresAt = Now.AddHours(8),
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ALiveAwakeSessionCanBeUsed()
    {
        DatabaseSessionStore.NotLive(Live(), Service, Now).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASleepingSessionCannotBeUsed()
    {
        var session = Live();
        session.AsleepAt = Now.AddMinutes(-2);

        DatabaseSessionStore.NotLive(session, Service, Now).ShouldNotBeNull().ShouldContain("asleep");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARevokedSessionIsReportedAsRevokedEvenWhileAsleep()
    {
        var session = Live();
        session.AsleepAt = Now.AddMinutes(-2);
        session.RevokedAt = Now.AddMinutes(-1);

        DatabaseSessionStore.NotLive(session, Service, Now).ShouldBe("it was revoked");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASleepingSessionCanBeWoken()
    {
        var session = Live();
        session.AsleepAt = Now.AddMinutes(-2);

        DatabaseSessionStore.NotWakeable(session, Now).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ASleepingSessionPastItsIdleEndCanStillBeWoken()
    {
        var session = Live();
        session.AsleepAt = Now.AddHours(-3);
        session.IdleExpiresAt = Now.AddHours(-2);

        DatabaseSessionStore.NotWakeable(session, Now).ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AnAwakeSessionCannotBeWoken()
    {
        DatabaseSessionStore.NotWakeable(Live(), Now).ShouldBe("it is not asleep");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void ARevokedOrExpiredOrMissingSessionCannotBeWoken()
    {
        var revoked = Live();
        revoked.AsleepAt = Now.AddMinutes(-2);
        revoked.RevokedAt = Now.AddMinutes(-1);
        var expired = Live();
        expired.AsleepAt = Now.AddMinutes(-2);
        expired.AbsoluteExpiresAt = Now.AddMinutes(-1);

        DatabaseSessionStore.NotWakeable(revoked, Now).ShouldBe("it was revoked");
        DatabaseSessionStore.NotWakeable(expired, Now).ShouldBe("it reached its absolute end");
        DatabaseSessionStore.NotWakeable(null, Now).ShouldBe("no session has this id");
    }
}
