using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// TypeCollection for schema relationship types.
/// </summary>
[TypeCollection(typeof(SchemaRelationshipTypeBase), typeof(ISchemaRelationshipType), typeof(SchemaRelationshipTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class SchemaRelationshipTypes : TypeCollectionBase<SchemaRelationshipTypeBase, ISchemaRelationshipType> { }
