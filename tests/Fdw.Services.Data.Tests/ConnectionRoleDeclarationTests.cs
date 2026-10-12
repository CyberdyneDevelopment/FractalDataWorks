using System;
using System.IO;
using System.Linq;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A host starts only with the connection roles it declares itself in <c>configurationSchema.json</c> (FDW-1121): a host
/// that leaves out the platform or server role, or names a connection its file does not declare, is refused with a
/// failure that names the host and the declaration, and nothing is defaulted in its place.
/// </summary>
public sealed class ConnectionRoleDeclarationTests
{
    private const string Host = "StartupTestHost";

    private static ConfigurationSchema Schema(ConnectionRolesConfiguration? roles, params string[] connections)
    {
        var schema = new ConfigurationSchema { ConnectionRoles = roles };
        foreach (var name in connections)
        {
            var connection = new Mock<IConnectionImplementationConfiguration>();
            connection.SetupGet(c => c.Name).Returns(name);
            schema.Connections.Add(connection.Object);
        }

        return schema;
    }

    private static string Reason(IGenericResult result) => string.Join(" | ", result.Messages.Select(message => message.Message));

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void AHostThatDeclaresNoRolesAtAllIsRefusedNamingTheHost()
    {
        var result = ConnectionRoleDeclaration.Declare(Schema(null), Host, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Reason(result).ShouldContain(Host);
        Reason(result).ShouldContain("ConnectionRoles");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void AHostThatLeavesOutTheServerRoleIsRefusedNamingTheRole()
    {
        var roles = new ConnectionRolesConfiguration { Platform = "PlatformConfiguration" };

        var result = ConnectionRoleDeclaration.Declare(Schema(roles, "PlatformConfiguration"), Host, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Reason(result).ShouldContain(Host);
        Reason(result).ShouldContain("ConnectionRoles.Server");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void AHostThatLeavesOutThePlatformRoleIsRefusedNamingTheRole()
    {
        var roles = new ConnectionRolesConfiguration { Server = "ServerConfiguration" };

        var result = ConnectionRoleDeclaration.Declare(Schema(roles, "ServerConfiguration"), Host, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Reason(result).ShouldContain("ConnectionRoles.Platform");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void ARoleThatNamesAConnectionTheFileDoesNotDeclareIsRefused()
    {
        var roles = new ConnectionRolesConfiguration { Platform = "PlatformConfiguration", Server = "ServerConfiguration" };

        var result = ConnectionRoleDeclaration.Declare(Schema(roles, "PlatformConfiguration"), Host, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Reason(result).ShouldContain("ServerConfiguration");
        Reason(result).ShouldContain(Host);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void ARoleAlreadyDeclaredAsADifferentConnectionIsRefused()
    {
        var roles = new ConnectionRolesConfiguration { Platform = "SomeOtherStore", Server = "ServerConfiguration" };

        var result = ConnectionRoleDeclaration.Declare(Schema(roles, "SomeOtherStore", "ServerConfiguration"), Host, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        Reason(result).ShouldContain("SomeOtherStore");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void AHostThatDeclaresPlatformAndServerStartsAndTheHoldersReadWhatItDeclared()
    {
        var roles = new ConnectionRolesConfiguration { Platform = "PlatformConfiguration", Server = "ServerConfiguration" };

        var result = ConnectionRoleDeclaration.Declare(Schema(roles, "PlatformConfiguration", "ServerConfiguration"), Host, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        PlatformConnection.Name.ShouldBe("PlatformConfiguration");
        ServerConnection.Name.ShouldBe("ServerConfiguration");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void ASchemaFileWithoutRolesIsRefusedWhenLoadedTheWayAHostLoadsIt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"configurationSchema-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "ConfigurationSchema": { "Connections": [], "DataStores": [] } }""");
        try
        {
            var result = ConnectionRoleDeclaration.Declare(ConfigurationSchemaLoader.Load(path), Host, NullLogger.Instance);

            result.IsFailure.ShouldBeTrue();
            Reason(result).ShouldContain(Host);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void ASchemaFileThatDeclaresRolesBindsThemFromJson()
    {
        var path = Path.Combine(Path.GetTempPath(), $"configurationSchema-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "ConfigurationSchema": { "Connections": [], "ConnectionRoles": { "Platform": "P", "Server": "S", "Auth": "A" } } }""");
        try
        {
            var roles = ConfigurationSchemaLoader.Load(path).ConnectionRoles;

            roles.ShouldNotBeNull();
            roles.Platform.ShouldBe("P");
            roles.Server.ShouldBe("S");
            roles.Auth.ShouldBe("A");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Startup")]
    public void ANameReadBeforeAnyHostDeclaredItThrowsNamingTheRole()
    {
        var undeclared = new DeclaredConnectionName("Auth");

        var thrown = Should.Throw<InvalidOperationException>(() => { _ = undeclared.Name; });

        thrown.Message.ShouldContain("ConnectionRoles.Auth");
        undeclared.IsDeclared.ShouldBeFalse();
    }
}
