using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

/// <summary>Proves an organization's default workspace id is what was set and is the empty id until one is (FDW-3668).</summary>
[Trait("Priority", "P1")]
public class OrganizationConfigurationTests
{
    [Fact]
    public void ANewOrganizationNamesNoDefaultWorkspaceUntilOneIsSet()
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
