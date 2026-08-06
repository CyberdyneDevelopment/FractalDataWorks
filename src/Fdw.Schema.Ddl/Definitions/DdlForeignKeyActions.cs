using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>TypeCollection for DDL foreign key actions.</summary>
[TypeCollection(typeof(DdlForeignKeyActionBase), typeof(IDdlForeignKeyAction), typeof(DdlForeignKeyActions))]
[ExcludeFromCodeCoverage]
public abstract partial class DdlForeignKeyActions : TypeCollectionBase<DdlForeignKeyActionBase, IDdlForeignKeyAction> { }
