using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// TypeCollection for foreign key referential action types.
/// </summary>
[TypeCollection(typeof(ForeignKeyActionBase), typeof(IForeignKeyAction), typeof(ForeignKeyActions))]
[ExcludeFromCodeCoverage]
public abstract partial class ForeignKeyActions : TypeCollectionBase<ForeignKeyActionBase, IForeignKeyAction> { }
