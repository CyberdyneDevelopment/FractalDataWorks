using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// TypeCollection for DDL command types (schema operations).
/// </summary>
[TypeCollection(typeof(DdlCommandTypeBase), typeof(IDdlCommandType), typeof(DdlCommandTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class DdlCommandTypes : TypeCollectionBase<DdlCommandTypeBase, IDdlCommandType> { }
