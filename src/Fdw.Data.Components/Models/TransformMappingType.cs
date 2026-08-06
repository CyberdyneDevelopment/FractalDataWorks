namespace Fdw.Data.Components.Models;

using Fdw.Collections.Attributes;

/// <summary>Source value is transformed by an expression before being written.</summary>
[TypeOption(typeof(MappingTypes), "Transform")]
public sealed class TransformMappingType : MappingTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TransformMappingType"/> class.</summary>
    public TransformMappingType() : base(2, "Transform") { }
}
