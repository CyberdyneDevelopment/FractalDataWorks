using System;
using System.IO;
using Fdw.Configuration;
using Shouldly;

namespace Fdw.Configuration.Tests;

/// <summary>
/// Tests for reading a table out of the ServerConfiguration store — the host-scoped settings that
/// would otherwise sit in appsettings.json.
/// </summary>
public sealed class ServerConfigurationStoreTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "fdw-servercfg-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Read_exposes_each_row_as_an_indexed_child_of_the_section()
    {
        Store("auth", "AuthenticationService", """
        [
          { "Name": "FdwAuthority", "ServiceOptionType": "LocalKey", "Enabled": true,
            "Authority": "https://issuer.example/", "Audience": "reference-api" },
          { "Name": "Partner", "ServiceOptionType": "JwtBearer", "Enabled": false,
            "Authority": "https://idp.example/" }
        ]
        """);

        var configuration = ServerConfigurationStore.Read(
            Schema(), "auth", "AuthenticationService", "AuthenticationServices");

        configuration["AuthenticationServices:0:Name"].ShouldBe("FdwAuthority");
        configuration["AuthenticationServices:0:ServiceOptionType"].ShouldBe("LocalKey");
        configuration["AuthenticationServices:0:Audience"].ShouldBe("reference-api");
        configuration["AuthenticationServices:1:Name"].ShouldBe("Partner");
    }

    [Fact]
    public void Read_renders_booleans_as_the_strings_configuration_binding_expects()
    {
        // The reader compares Enabled against "true" as a string, so a JSON boolean has to arrive
        // as one. Rendered by hand rather than via ToString(), which yields "True" and would make
        // every declared entry silently disabled.
        Store("auth", "AuthenticationService", """
        [ { "Name": "On", "Enabled": true }, { "Name": "Off", "Enabled": false } ]
        """);

        var configuration = ServerConfigurationStore.Read(
            Schema(), "auth", "AuthenticationService", "AuthenticationServices");

        configuration["AuthenticationServices:0:Enabled"].ShouldBe("true");
        configuration["AuthenticationServices:1:Enabled"].ShouldBe("false");
    }

    [Fact]
    public void Read_flattens_nested_objects_and_arrays_into_colon_delimited_keys()
    {
        Store("auth", "AuthenticationService", """
        [ { "Name": "Partner", "Roles": [ "Admin", "Reader" ], "Limits": { "Seconds": 30 } } ]
        """);

        var configuration = ServerConfigurationStore.Read(
            Schema(), "auth", "AuthenticationService", "AuthenticationServices");

        configuration["AuthenticationServices:0:Roles:0"].ShouldBe("Admin");
        configuration["AuthenticationServices:0:Roles:1"].ShouldBe("Reader");
        configuration["AuthenticationServices:0:Limits:Seconds"].ShouldBe("30");
    }

    [Fact]
    public void Read_returns_empty_when_the_table_file_is_absent()
    {
        // Empty rather than throwing: a host that declares no entries is a host with none, and its
        // consumer already reports that in its own terms. Failing here would report a missing file
        // for what is a missing declaration.
        ServerConfigurationStore
            .Read(Schema(), "auth", "NoSuchTable", "AuthenticationServices")
            .GetSection("AuthenticationServices").GetChildren().ShouldBeEmpty();
    }

    [Fact]
    public void Read_returns_empty_when_no_ServerConfiguration_connection_is_declared()
    {
        var schema = Path.Combine(_root, "no-connection.json");
        Directory.CreateDirectory(_root);
        File.WriteAllText(schema, """
        { "ConfigurationSchema": { "Connections": [ { "Name": "PlatformConfiguration" } ] } }
        """);

        ServerConfigurationStore
            .Read(schema, "auth", "AuthenticationService", "AuthenticationServices")
            .GetSection("AuthenticationServices").GetChildren().ShouldBeEmpty();
    }

    [Fact]
    public void Read_returns_empty_when_the_schema_file_is_absent()
    {
        ServerConfigurationStore
            .Read(Path.Combine(_root, "missing.json"), "auth", "AuthenticationService", "Any")
            .GetSection("Any").GetChildren().ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private string Schema()
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "configurationSchema.json");
        File.WriteAllText(path, $$"""
        {
          "ConfigurationSchema": {
            "Connections": [
              { "Name": "ServerConfiguration", "ServiceOptionType": "FileSystem",
                "Configuration": { "Root": {{System.Text.Json.JsonSerializer.Serialize(_root)}} } }
            ]
          }
        }
        """);
        return path;
    }

    private void Store(string path, string table, string json)
    {
        var folder = Path.Combine(_root, path);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, table + ".json"), json);
    }
}
