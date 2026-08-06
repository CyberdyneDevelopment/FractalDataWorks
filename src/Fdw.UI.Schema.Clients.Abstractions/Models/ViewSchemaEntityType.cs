using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Schema.Clients.Models;

/// <summary>A view in the database.</summary>
[TypeOption(typeof(SchemaEntityTypes), "View")]
[ExcludeFromCodeCoverage]
public sealed class ViewSchemaEntityType : SchemaEntityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ViewSchemaEntityType"/>.</summary>
    public ViewSchemaEntityType() : base(2, "View") { }
}
