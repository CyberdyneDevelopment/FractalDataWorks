using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Rename an existing column.</summary>
[TypeOption(typeof(AlterTableOperationTypes), "RenameColumn")]
[ExcludeFromCodeCoverage]
public sealed class RenameColumnAlterTableOperationType : AlterTableOperationTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RenameColumnAlterTableOperationType"/>.</summary>
    public RenameColumnAlterTableOperationType() : base(4, "RenameColumn") { }
}
