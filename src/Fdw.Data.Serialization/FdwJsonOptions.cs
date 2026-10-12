using System.Text.Json;
using System.Text.Json.Serialization;
using Fdw.Data.Serialization.Converters;

namespace Fdw.Data.Serialization;

/// <summary>
/// Provides pre-configured JsonSerializerOptions for Fdw types.
/// </summary>
public static class FdwJsonOptions
{
    /// <summary>
    /// Gets default serializer options with all Fdw converters registered.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null, // Use PascalCase to match C# property names
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new FieldTypeConverter(),
                new FieldConverter(),
                new ContainerSchemaConverter(),
                new ContainerConverter()
            }
        };

        return options;
    }
}
