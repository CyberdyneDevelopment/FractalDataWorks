using System;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Authentication.Abstractions.Tests.Security;

/// <summary>
/// Tests for <see cref="AuthConstants"/> — the reserved deny-everywhere principal identity used by
/// <c>MsSqlConnection.BuildSessionContextPlan</c> whenever no real, Guid-identified principal is
/// established for the current call flow.
/// </summary>
public class AuthConstantsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoAccessPrincipalIdIsNotGuidEmpty()
    {
        // Why: Guid.Empty could plausibly appear by accident from an uninitialized field — the
        // reserved principal must be unambiguously deliberate, never confusable with a default value.
        AuthConstants.NoAccessPrincipalId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoAccessPrincipalIdIsTheFixedReservedValue()
    {
        // Why: this value is a documented, stable sentinel — it must never drift, since seed data
        // and future migrations rely on it never colliding with an app-minted Guid.CreateVersion7().
        AuthConstants.NoAccessPrincipalId.ShouldBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }
}
