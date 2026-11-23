using System.Collections.Generic;
using Fdw.Services.Descriptions.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Descriptions.Tests;

/// <summary>FDW-6208: every description type states the permission that reads a thing of it; one that does not stops the host when it initializes, with a coded message naming it.</summary>
[Trait("Category", "Descriptions")]
public sealed class DescriptionTypeRequirementsTests
{
    private sealed class Stated(string name, string permission) : DescriptionTypeBase(name, permission, hasOwner: false);

    [Fact]
    [Trait("Priority", "P0")]
    public void TheShippedTypesAllStateTheirReadPermission()
    {
        DescriptionTypeRequirements.ReadPermissionsStated(DescriptionTypes.All(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [InlineData("")]
    [InlineData("   ")]
    public void ATypeThatStatesNoReadPermissionStopsTheHostNamingTheType(string permission)
    {
        IDescriptionType[] types = [new Stated("Fine", "things:read"), new Stated("Silent", permission)];

        var checkedTypes = DescriptionTypeRequirements.ReadPermissionsStated(types, NullLogger.Instance);

        checkedTypes.IsFailure.ShouldBeTrue();
        checkedTypes.CurrentMessage.ShouldNotBeNull().ShouldContain("Silent");
    }

    [Fact]
    [Trait("Priority", "P0")]
    public void ATypeBuiltWithNoReadPermissionDoesNotThrowBecauseTheHostChecksItWhenItInitializes()
    {
        Should.NotThrow(() => new Stated("Silent", string.Empty));
    }
}
