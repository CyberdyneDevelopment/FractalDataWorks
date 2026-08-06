using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>Standard text input.</summary>
[TypeOption(typeof(ConfigurationPropertyTypes), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextConfigurationPropertyType : ConfigurationPropertyTypeBasePayload
{
    /// <summary>Initializes a new instance of <see cref="TextConfigurationPropertyType"/>.</summary>
    public TextConfigurationPropertyType() : base(1, "Text") { }
}
