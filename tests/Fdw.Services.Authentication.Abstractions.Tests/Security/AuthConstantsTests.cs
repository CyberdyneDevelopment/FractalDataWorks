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
        AuthConstants.NoAccessPrincipalId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void NoAccessPrincipalIdIsTheFixedReservedValue()
    {
        AuthConstants.NoAccessPrincipalId.ShouldBe(Guid.Parse("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"));
    }
}
