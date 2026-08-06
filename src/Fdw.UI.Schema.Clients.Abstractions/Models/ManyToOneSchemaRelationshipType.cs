using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>A many-to-one relationship where many foreign entities relate to one primary entity.</summary>
[TypeOption(typeof(SchemaRelationshipTypes), "ManyToOne")]
[ExcludeFromCodeCoverage]
public sealed class ManyToOneSchemaRelationshipType : SchemaRelationshipTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ManyToOneSchemaRelationshipType"/>.</summary>
    public ManyToOneSchemaRelationshipType() : base(3, "ManyToOne") { }
}
