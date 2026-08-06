using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>A one-to-many relationship where one primary entity relates to many foreign entities.</summary>
[TypeOption(typeof(SchemaRelationshipTypes), "OneToMany")]
[ExcludeFromCodeCoverage]
public sealed class OneToManySchemaRelationshipType : SchemaRelationshipTypeBase
{
    /// <summary>Initializes a new instance of <see cref="OneToManySchemaRelationshipType"/>.</summary>
    public OneToManySchemaRelationshipType() : base(2, "OneToMany") { }
}
