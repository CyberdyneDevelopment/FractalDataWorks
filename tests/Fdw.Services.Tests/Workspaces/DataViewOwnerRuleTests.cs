using System;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-2328: the framework's answer to who may change or delete a data view is the person who saved it. A developer replaces the
/// question by overriding <c>MayChange</c> on the update and delete endpoints.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class DataViewOwnerRuleTests
{
    private static readonly Guid Saver = Guid.NewGuid();

    [Fact]
    public void TheSaverMayChangeTheView()
    {
        DataViewStore.OwnedByCaller(Accessor(Saver.ToString()), View(Saver), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AnotherPersonMayNot()
    {
        var result = DataViewStore.OwnedByCaller(Accessor(Guid.NewGuid().ToString()), View(Saver), NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeOfType<DataViewWriteNotPermittedCode>();
    }

    [Fact]
    public void ACallerWhoIsNotAGuidIsRefusedAsUnresolved()
    {
        var result = DataViewStore.OwnedByCaller(Accessor("not-a-guid"), View(Saver), NullLogger.Instance);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeOfType<WorkspaceOwnerUnresolvedCode>();
    }

    private static IAuthenticationContextAccessor Accessor(string userId)
    {
        var caller = new Mock<IAuthenticationContext>();
        caller.SetupGet(c => c.UserId).Returns(userId);
        return new AuthenticationContextAccessor { Current = caller.Object };
    }

    private static IDataViewDataSetVariationConfiguration View(Guid owner)
        => new DataViewDataSetVariationConfiguration { Name = "weekly", OwnerUserId = owner };
}
