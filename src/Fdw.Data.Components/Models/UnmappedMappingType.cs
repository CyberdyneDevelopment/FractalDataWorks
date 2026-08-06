namespace Fdw.Data.Components.Models;

using Fdw.Collections.Attributes;

/// <summary>No mapping has been defined for this field.</summary>
[TypeOption(typeof(MappingTypes), "Unmapped")]
public sealed class UnmappedMappingType : MappingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UnmappedMappingType"/> class.</summary>
    public UnmappedMappingType() : base(5, "Unmapped") { }
}
