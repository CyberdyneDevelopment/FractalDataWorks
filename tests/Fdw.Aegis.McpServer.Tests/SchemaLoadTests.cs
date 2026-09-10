using System;
using System.IO;
using Fdw.Services.Connections.TestDouble;

namespace Fdw.Aegis.McpServer.Tests;

/// <summary>
/// Covers the REAL startup deserialization path — <c>AegisHostRegistration.LoadSchema</c> plus the
/// connection converter — that <c>Program.cs</c> runs at boot. The non-exposure suite builds its
/// schema in-code, so this is the only place the polymorphic <c>Implementation</c> discriminator
/// dispatch is exercised end to end. The declared commands are not in the schema: they are the
/// AegisCommand domain, read through its provider.
/// </summary>
[Trait("Category", "Security")]
public sealed class SchemaLoadTests
{
    [Fact]
    public void LoadSchemaDeserializesThePolymorphicConnectionBody()
    {
        const string json = """
        {
          "ConfigurationSchema": {
            "Connections": [
              { "Name": "synthetic-echo", "Implementation": "MockConnection", "Configuration": { "Root": "config-data" } }
            ]
          }
        }
        """;

        var path = Path.Combine(Path.GetTempPath(), $"aegis-schema-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        try
        {
            var schema = AegisHostRegistration.LoadSchema(path);

            schema.Connections.Count.ShouldBe(1);

            // The discriminator dispatched to the correct implementation, stamped with the entry's name.
            var connection = schema.Connections[0].ShouldBeOfType<MockConnectionConfiguration>();
            connection.Name.ShouldBe("synthetic-echo");
            connection.Implementation.ShouldBe("MockConnection");
            connection.Root.ShouldBe("config-data");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
