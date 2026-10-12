using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Base class for configuration property type DTOs for UI rendering.
/// </summary>
public abstract class ConfigurationPropertyTypeDtoBase : TypeOptionBase<ConfigurationPropertyTypeDtoBase>, IConfigurationPropertyTypeDto
{
    /// <summary>
    /// Initializes a new instance of <see cref="ConfigurationPropertyTypeDtoBase"/>.
    /// </summary>
    protected ConfigurationPropertyTypeDtoBase(string name) : base(name) { }
}
