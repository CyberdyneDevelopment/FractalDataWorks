using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>A many-to-many relationship between the two entities.</summary>
[TypeOption(typeof(SchemaRelationshipTypes), "ManyToMany")]
[ExcludeFromCodeCoverage]
public sealed class ManyToManySchemaRelationshipType : SchemaRelationshipTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ManyToManySchemaRelationshipType"/>.</summary>
    public ManyToManySchemaRelationshipType() : base(4, "ManyToMany") { }
}
