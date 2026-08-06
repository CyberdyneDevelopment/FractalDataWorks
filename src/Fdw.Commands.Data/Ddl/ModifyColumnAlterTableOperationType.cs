using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Modify an existing column's definition.</summary>
[TypeOption(typeof(AlterTableOperationTypes), "ModifyColumn")]
[ExcludeFromCodeCoverage]
public sealed class ModifyColumnAlterTableOperationType : AlterTableOperationTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ModifyColumnAlterTableOperationType"/>.</summary>
    public ModifyColumnAlterTableOperationType() : base(3, "ModifyColumn") { }
}
