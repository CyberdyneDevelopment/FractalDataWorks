using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>A one-to-one relationship between the two entities.</summary>
[TypeOption(typeof(SchemaRelationshipTypes), "OneToOne")]
[ExcludeFromCodeCoverage]
public sealed class OneToOneSchemaRelationshipType : SchemaRelationshipTypeBase
{
    /// <summary>Initializes a new instance of <see cref="OneToOneSchemaRelationshipType"/>.</summary>
    public OneToOneSchemaRelationshipType() : base(1, "OneToOne") { }
}
