namespace Fdw.Data.Components.Models;

using Fdw.Collections.Attributes;

/// <summary>A fixed constant value is written regardless of the source field.</summary>
[TypeOption(typeof(MappingTypes), "Constant")]
public sealed class ConstantMappingType : MappingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConstantMappingType"/> class.</summary>
    public ConstantMappingType() : base(3, "Constant") { }
}
