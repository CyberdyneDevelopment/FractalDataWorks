using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>TypeCollection for DDL operation types.</summary>
[TypeCollection(typeof(DdlCommandTypeBase), typeof(IDdlCommandType), typeof(DdlCommandTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class DdlCommandTypes : TypeCollectionBase<DdlCommandTypeBase, IDdlCommandType> { }
