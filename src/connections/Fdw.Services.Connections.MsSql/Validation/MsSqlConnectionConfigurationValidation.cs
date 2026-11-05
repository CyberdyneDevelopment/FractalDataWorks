using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.MsSql;
using Fdw.Validation.Configuration;

namespace Fdw.Services.Connections.MsSql.Validation;

/// <summary>States that <see cref="MsSqlConnectionConfiguration"/> is checked by <see cref="MsSqlConnectionConfigurationValidator"/> when it is written (FDW-3541).</summary>
/// <remarks>The connection endpoints run the same validator on create and update through the connection type; this runs it again on any write through the domain provider.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "MsSqlConnectionConfiguration")]
public sealed class MsSqlConnectionConfigurationValidation : ValidatedConfiguration<MsSqlConnectionConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="MsSqlConnectionConfigurationValidation"/> class.</summary>
    public MsSqlConnectionConfigurationValidation()
        : base("MsSqlConnectionConfiguration", new MsSqlConnectionConfigurationValidator())
    {
    }
}
