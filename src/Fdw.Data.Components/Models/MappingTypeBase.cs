namespace Fdw.Data.Components.Models;

using Fdw.Collections;

/// <summary>Base class for all field mapping type options.</summary>
public abstract class MappingTypeBase : TypeOptionBase<int, MappingTypeBase>, IMappingType
{
    /// <summary>Initializes a new instance of the <see cref="MappingTypeBase"/> class.</summary>
    protected MappingTypeBase(int id, string name) : base(id, name) { }
}
