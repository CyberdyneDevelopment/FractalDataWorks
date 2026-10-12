using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>
/// TypeCollection for schema entity types.
/// </summary>
[TypeCollection(typeof(SchemaEntityTypeBase), typeof(ISchemaEntityType), typeof(SchemaEntityTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class SchemaEntityTypes : TypeCollectionBase<SchemaEntityTypeBase, ISchemaEntityType> { }
