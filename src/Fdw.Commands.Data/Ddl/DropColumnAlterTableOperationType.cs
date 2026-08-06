using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Drop an existing column.</summary>
[TypeOption(typeof(AlterTableOperationTypes), "DropColumn")]
[ExcludeFromCodeCoverage]
public sealed class DropColumnAlterTableOperationType : AlterTableOperationTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropColumnAlterTableOperationType"/>.</summary>
    public DropColumnAlterTableOperationType() : base(2, "DropColumn") { }
}
