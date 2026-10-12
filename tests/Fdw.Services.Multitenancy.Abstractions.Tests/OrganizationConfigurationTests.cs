using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

/// <summary>Proves an organization names no default workspace until one is set (FDW-1060).</summary>
[Trait("Priority", "P1")]
public class OrganizationConfigurationTests
{
    [Fact]
    public void ANewOrganizationNamesNoDefaultWorkspace()
    {
        new OrganizationConfiguration().DefaultWorkspaceId.ShouldBeNull();
    }

    [Fact]
    public void TheDefaultWorkspaceIdIsWhatWasSet()
    {
        var workspace = Guid.NewGuid();

        new OrganizationConfiguration { DefaultWorkspaceId = workspace }.DefaultWorkspaceId.ShouldBe(workspace);
    }
}
