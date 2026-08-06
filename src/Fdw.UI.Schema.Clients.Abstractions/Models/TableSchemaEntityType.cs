using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>A base table in the database.</summary>
[TypeOption(typeof(SchemaEntityTypes), "Table")]
[ExcludeFromCodeCoverage]
public sealed class TableSchemaEntityType : SchemaEntityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="TableSchemaEntityType"/>.</summary>
    public TableSchemaEntityType() : base(1, "Table") { }
}
