using Fdw.Collections;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Base class for schema entity types.
/// </summary>
public abstract class SchemaEntityTypeBase : TypeOptionBase<SchemaEntityTypeBase>, ISchemaEntityType
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaEntityTypeBase"/>.
    /// </summary>
    protected SchemaEntityTypeBase(string name) : base(name) { }
}
