using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Drop a constraint.</summary>
[TypeOption(typeof(AlterTableOperationTypes), "DropConstraint")]
[ExcludeFromCodeCoverage]
public sealed class DropConstraintAlterTableOperationType : AlterTableOperationTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropConstraintAlterTableOperationType"/>.</summary>
    public DropConstraintAlterTableOperationType() : base(6, "DropConstraint") { }
}
