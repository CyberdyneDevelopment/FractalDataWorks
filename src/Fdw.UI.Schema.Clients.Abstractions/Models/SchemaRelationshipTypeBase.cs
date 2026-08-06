using Fdw.Collections;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// Base class for schema relationship types.
/// </summary>
public abstract class SchemaRelationshipTypeBase : TypeOptionBase<int, SchemaRelationshipTypeBase>, ISchemaRelationshipType
{
    /// <summary>
    /// Initializes a new instance of <see cref="SchemaRelationshipTypeBase"/>.
    /// </summary>
    protected SchemaRelationshipTypeBase(int id, string name) : base(id, name) { }
}
