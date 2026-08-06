namespace Fdw.Data.Components.Models;

using Fdw.Collections.Attributes;

/// <summary>Source field value is copied directly to the target field.</summary>
[TypeOption(typeof(MappingTypes), "Direct")]
public sealed class DirectMappingType : MappingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DirectMappingType"/> class.</summary>
    public DirectMappingType() : base(1, "Direct") { }
}
