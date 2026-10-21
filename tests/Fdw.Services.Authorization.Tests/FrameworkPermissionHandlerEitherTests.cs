using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.Services.Authorization.Tests;

/// <summary>
/// A requirement that two permissions can satisfy (FDW-2246): holding either opens the endpoint, holding neither does not, and a check that fails
/// does not hide a permission that is held.
/// </summary>
public sealed class FrameworkPermissionHandlerEitherTests
{
    private readonly Mock<IFrameworkAuthorizationService> _authorization = new();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HoldingOnlyTheSecondPermissionSucceeds()
    {
        Answer("ui-feedback", "read", false);
        Answer("ui-feedback", "write", true);
        var context = Context();

        await Handler().HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task HoldingNeitherPermissionDoesNotSucceed()
    {
        Answer("ui-feedback", "read", false);
        Answer("ui-feedback", "write", false);
        var context = Context();

        await Handler().HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFailedCheckOfTheFirstPermissionDoesNotHideTheSecondOneBeingHeld()
    {
        _authorization
            .Setup(s => s.Authorize(It.IsAny<IAuthenticationContext>(), "ui-feedback", "read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Failure(Fdw.Services.Authorization.Logging.AuthorizationLog.AuthorizationContextNull(NullLogger.Instance)));
        Answer("ui-feedback", "write", true);
        var context = Context();

        await Handler().HandleAsync(context);

        context.HasSucceeded.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task AFailedCheckOfEveryPermissionDoesNotSucceed()
    {
        var failure = GenericResult<bool>.Failure(Fdw.Services.Authorization.Logging.AuthorizationLog.AuthorizationContextNull(NullLogger.Instance));
        _authorization
            .Setup(s => s.Authorize(It.IsAny<IAuthenticationContext>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failure);
        var context = Context();

        await Handler().HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ARequirementWithAlternativesNamesAtLeastOne()
    {
        Should.Throw<System.ArgumentException>(() => new FdwPermissionRequirement("ui-feedback", "read", []));
    }

    private void Answer(string resource, string action, bool held) =>
        _authorization
            .Setup(s => s.Authorize(It.IsAny<IAuthenticationContext>(), resource, action, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(held));

    private FrameworkPermissionHandler Handler() => new(_authorization.Object, NullLogger<FrameworkPermissionHandler>.Instance);

    private static AuthorizationHandlerContext Context() =>
        new(
            new List<IAuthorizationRequirement> { new FdwPermissionRequirement("ui-feedback", "read", [("ui-feedback", "write")]) },
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], authenticationType: "test")),
            resource: null);
}
