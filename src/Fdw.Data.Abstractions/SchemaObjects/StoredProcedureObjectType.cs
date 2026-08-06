using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for stored procedures.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "StoredProcedure", RestrictToCurrentCompilation = true)]
public sealed class StoredProcedureObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// MudBlazor icon for stored procedures.
    /// </summary>
    public const string ProcedureIcon = "code"; // Icons.Material.Filled.Code

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedureObjectType"/> class.
    /// </summary>
    public StoredProcedureObjectType()
        : base(
            id: 5,
            name: "StoredProcedure",
            icon: ProcedureIcon,
            color: "Warning",
            cssClass: "schema-procedure",
            canHaveChildren: false,
            sortOrder: 5)
    {
    }
}
