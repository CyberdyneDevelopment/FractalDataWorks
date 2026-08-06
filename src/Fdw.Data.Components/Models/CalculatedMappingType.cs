namespace Fdw.Data.Components.Models;

using Fdw.Collections.Attributes;

/// <summary>Value is computed from a calculation pipeline.</summary>
[TypeOption(typeof(MappingTypes), "Calculated")]
public sealed class CalculatedMappingType : MappingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculatedMappingType"/> class.</summary>
    public CalculatedMappingType() : base(4, "Calculated") { }
}
