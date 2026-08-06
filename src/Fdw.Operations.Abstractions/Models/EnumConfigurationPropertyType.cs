using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Dropdown selection from AllowedValues.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Enum")]
[ExcludeFromCodeCoverage]
public sealed class EnumConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="EnumConfigurationPropertyType"/>.</summary>
    public EnumConfigurationPropertyType() : base(6, "Enum") { }
}
