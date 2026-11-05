using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>Refuse a change to a referenced row while referencing rows exist, checked at once rather than at the end of the statement.</summary>
/// <remarks>SQL Server has no RESTRICT; its DDL generator refuses this action rather than writing another.</remarks>
[TypeOption(typeof(ForeignKeyActions), "Restrict")]
[ExcludeFromCodeCoverage]
public sealed class RestrictForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="RestrictForeignKeyAction"/>.</summary>
    public RestrictForeignKeyAction() : base("Restrict", "RESTRICT") { }
}
