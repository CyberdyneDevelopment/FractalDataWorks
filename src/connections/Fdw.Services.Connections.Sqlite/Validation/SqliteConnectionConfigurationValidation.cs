using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Sqlite;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.Sqlite.Validation;

/// <summary>States that <see cref="SqliteConnectionConfiguration"/> is checked by <see cref="SqliteConnectionConfigurationValidator"/> when it is written (FDW-3541).</summary>
/// <remarks>The connection endpoints run the same validator on create and update through the connection type; this runs it again on any write through the domain provider.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "SqliteConnectionConfiguration")]
public sealed class SqliteConnectionConfigurationValidation : ValidatedConfiguration<SqliteConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SqliteConnectionConfigurationValidation"/> class.</summary>
    public SqliteConnectionConfigurationValidation()
        : base("SqliteConnectionConfiguration", new SqliteConnectionConfigurationValidator())
    {
    }
}
