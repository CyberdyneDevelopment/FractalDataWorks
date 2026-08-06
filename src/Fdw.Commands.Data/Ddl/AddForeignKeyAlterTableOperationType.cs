using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Add a foreign key constraint.</summary>
[TypeOption(typeof(AlterTableOperationTypes), "AddForeignKey")]
[ExcludeFromCodeCoverage]
public sealed class AddForeignKeyAlterTableOperationType : AlterTableOperationTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AddForeignKeyAlterTableOperationType"/>.</summary>
    public AddForeignKeyAlterTableOperationType() : base(5, "AddForeignKey") { }
}
