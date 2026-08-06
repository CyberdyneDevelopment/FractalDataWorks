using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Add a new column.</summary>
[TypeOption(typeof(AlterTableOperationTypes), "AddColumn")]
[ExcludeFromCodeCoverage]
public sealed class AddColumnAlterTableOperationType : AlterTableOperationTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AddColumnAlterTableOperationType"/>.</summary>
    public AddColumnAlterTableOperationType() : base(1, "AddColumn") { }
}
