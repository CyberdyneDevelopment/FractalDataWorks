using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// TypeCollection for foreign key referential actions.
/// </summary>
[TypeCollection(typeof(ForeignKeyActionBase), typeof(IForeignKeyAction), typeof(ForeignKeyActions))]
[ExcludeFromCodeCoverage]
public abstract partial class ForeignKeyActions : TypeCollectionBase<ForeignKeyActionBase, IForeignKeyAction> { }
