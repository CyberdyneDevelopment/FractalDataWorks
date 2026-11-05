using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.PostgreSql;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.PostgreSql.Validation;

/// <summary>States that <see cref="PostgreSqlConnectionConfiguration"/> is checked by <see cref="PostgreSqlConnectionConfigurationValidator"/> when it is written (FDW-3541).</summary>
/// <remarks>The connection endpoints run the same validator on create and update through the connection type; this runs it again on any write through the domain provider.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "PostgreSqlConnectionConfiguration")]
public sealed class PostgreSqlConnectionConfigurationValidation : ValidatedConfiguration<PostgreSqlConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="PostgreSqlConnectionConfigurationValidation"/> class.</summary>
    public PostgreSqlConnectionConfigurationValidation()
        : base("PostgreSqlConnectionConfiguration", new PostgreSqlConnectionConfigurationValidator())
    {
    }
}
