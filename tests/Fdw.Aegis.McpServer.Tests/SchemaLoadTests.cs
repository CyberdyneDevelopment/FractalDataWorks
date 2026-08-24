using System;
using System.IO;
using Fdw.Aegis.Configuration;
using Fdw.Services.SecretManagers.TestDouble;
using Fdw.Aegis.McpServer.Tests;

namespace Fdw.Aegis.McpServer.Tests;

/// <summary>
/// Covers the REAL startup deserialization path — <c>AegisHostRegistration.LoadSchema</c> plus the
/// three STJ converters (Command / SecretManager / Connection) — that <c>Program.cs</c> runs at boot.
/// The non-exposure suite builds its schema in-code, so this is the only place the polymorphic
/// <c>ServiceOptionType</c> discriminator dispatch is exercised end to end.
/// </summary>
[Trait("Category", "Security")]
public sealed class SchemaLoadTests
{
    [Fact]
    public void LoadSchemaDeserializesThePolymorphicCommandAndSecretBodies()
    {
        const string json = """
        {
          "ConfigurationSchema": {
            "SecretManagers": [
              { "Name": "EnvSecrets", "ServiceOptionType": "Synthetic", "Configuration": { "Prefix": "FDW_SECRET_" } }
            ],
            "Connections": [
              { "Name": "synthetic-echo", "ServiceOptionType": "MockConnection", "Configuration": { "Root": "config-data" } }
            ],
            "Commands": [
              {
                "Name": "echo_credential",
                "ConnectionName": "synthetic-echo",
                "ServiceOptionType": "PreApproved",
                "Configuration": {
                  "SecretManagerName": "EnvSecrets",
                  "SecretKeyName": "AEGIS_SYNTHETIC_TOKEN",
                  "ParameterAllowList": [ { "ParameterName": "mode", "PermittedValues": [ "echo" ], "Required": true } ]
                }
              }
            ]
          }
        }
        """;

        var path = Path.Combine(Path.GetTempPath(), $"aegis-schema-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        try
        {
            var schema = AegisHostRegistration.LoadSchema(path);

            schema.Commands.Count.ShouldBe(1);
            var command = schema.Commands[0];
            command.Name.ShouldBe("echo_credential");
            command.ServiceOptionType.ShouldBe("PreApproved");

            // The discriminator dispatched to the correct typed body — the whole point of the converter.
            var preApproved = command.Configuration.ShouldBeOfType<PreApprovedCommandConfiguration>();
            preApproved.SecretManagerName.ShouldBe("EnvSecrets");
            preApproved.SecretKeyName.ShouldBe("AEGIS_SYNTHETIC_TOKEN");
            preApproved.ParameterAllowList.Count.ShouldBe(1);
            preApproved.ParameterAllowList[0].ParameterName.ShouldBe("mode");
            preApproved.ParameterAllowList[0].PermittedValues.ShouldContain("echo");

            schema.SecretManagers.Count.ShouldBe(1);
            schema.SecretManagers[0].Configuration.ShouldBeOfType<SyntheticSecretManagerConfiguration>();

            // Why assert this too: the summary claims all THREE converters are covered, and the
            // connection body is the one that silently bound to null before the converters failed loud.
            schema.Connections.Count.ShouldBe(1);
            schema.Connections[0].Configuration.ShouldBeOfType<MockConnectionConfiguration>()
                .Root.ShouldBe("config-data");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
