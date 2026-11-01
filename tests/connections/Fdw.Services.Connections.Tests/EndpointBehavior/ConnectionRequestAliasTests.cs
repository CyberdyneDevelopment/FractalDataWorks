using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests.EndpointBehavior;

/// <summary>
/// The deprecated flat request shape still works: its fields become aliases for a type's own
/// configuration properties, and the detail response still fills the flat fields.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ConnectionRequestAliasTests
{
    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CreateBodyCarriesOnlyTheFlatFieldsTheCallerSet()
    {
        var body = ConnectionRequestBody.ForCreate(new CreateConnectionRequest
        {
            Name = "ops", ServiceType = "MsSql", Server = "db", Database = "Ops",
            Authentication = new Dictionary<string, string?> { ["Username"] = "sa" },
        });

        body.GetProperty("server").GetString().ShouldBe("db");
        body.GetProperty("database").GetString().ShouldBe("Ops");
        body.GetProperty("authentication").GetProperty("Username").GetString().ShouldBe("sa");
        body.TryGetProperty("port", out _).ShouldBeFalse();
        body.TryGetProperty("encrypt", out _).ShouldBeFalse();
        body.TryGetProperty("baseUrl", out _).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void CreateBodyLetsTheTypedConfigurationWin()
    {
        var body = ConnectionRequestBody.ForCreate(new CreateConnectionRequest
        {
            Name = "ops", ServiceType = "MsSql", Server = "old",
            Configuration = Json("""{ "Server": "new", "port": 1444 }"""),
        });

        body.GetProperty("Server").GetString().ShouldBe("new");
        body.TryGetProperty("server", out _).ShouldBeFalse();
        body.GetProperty("port").GetInt32().ShouldBe(1444);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void UpdateBodyCarriesOnlyTheMembersSent()
    {
        var body = ConnectionRequestBody.ForUpdate(new UpdateConnectionRequest { Name = "files", BaseUrl = "/data/in" });

        body.GetProperty("baseUrl").GetString().ShouldBe("/data/in");
        body.EnumerateObject().Count().ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void AFlatCreateRequestBindsThroughTheTypesAlias()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        behavior.Alias("server", nameof(SftpTestConfiguration.Host));

        var bound = behavior.Bind(
            ConnectionRequestBody.ForCreate(new CreateConnectionRequest { Name = "partner", ServiceType = "Sftp", Server = "files.example.com" }),
            NullLogger.Instance);

        var sftp = bound.Value.ShouldBeOfType<SftpTestConfiguration>();
        sftp.Host.ShouldBe("files.example.com");
        // The flat request's SQL Server port (1433) is not written, so the type's own default stands.
        sftp.Port.ShouldBe(22);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public void TheDetailFillsTheFlatFieldsThroughTheTypesAlias()
    {
        var behavior = new ConnectionEndpointBehavior(typeof(SftpTestConfiguration));
        behavior.Alias("server", nameof(SftpTestConfiguration.Host));
        var type = new Mock<IConnectionType>();
        type.SetupGet(t => t.Name).Returns("Sftp");
        type.SetupGet(t => t.EndpointBehavior).Returns(behavior);
        // The test's configuration names no authentication method, so the type cannot say which credential fields it has.
        type.Setup(t => t.CredentialSlots(It.IsAny<IConnectionImplementationConfiguration>()))
            .Returns(GenericResult<ConnectionCredentialSlots>.Failure(Mock.Of<IGenericMessage>()));
        var created = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);
        var modified = new DateTimeOffset(2026, 4, 2, 9, 30, 0, TimeSpan.Zero);
        var sftp = new SftpTestConfiguration { Id = Guid.NewGuid(), Name = "partner", Host = "files.example.com", Port = 2222, SecretKeyName = "k", CreateDate = created, ModifyDate = modified };

        var detail = ConnectionDetailMapper.Map(type.Object, sftp, NullLogger.Instance);

        detail.IsSuccess.ShouldBeTrue();
        var shown = detail.Value!;
        shown.ServiceType.ShouldBe("Sftp");
        shown.Name.ShouldBe("partner");
        shown.CreatedAt.ShouldBe(created);
        shown.UpdatedAt.ShouldBe(modified);
        shown.Server.ShouldBe("files.example.com");
        shown.Port.ShouldBe(2222);
        shown.Database.ShouldBe(string.Empty);
        var settings = shown.Configuration!;
        settings["host"]!.GetValue<string>().ShouldBe("files.example.com");
        settings["secretKeyName"]!.GetValue<string>().ShouldBe(ConnectionSecretMask.Value);
        shown.Credentials.ShouldBeNull();
    }
}
