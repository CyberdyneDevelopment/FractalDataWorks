using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>TypeCollection for ALTER TABLE operation types.</summary>
[TypeCollection(typeof(AlterTableOperationTypeBase), typeof(IAlterTableOperationType), typeof(AlterTableOperationTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class AlterTableOperationTypes : TypeCollectionBase<AlterTableOperationTypeBase, IAlterTableOperationType> { }
