using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The vectorized commands: those only a store that holds vectors answers. Kept apart from <see cref="DataCommands"/>,
/// which every connection answers (FDW-1354).
/// </summary>
[TypeCollection(typeof(VectorizedDataCommandBase), typeof(IVectorizedDataCommand), typeof(VectorizedDataCommands))]
[ExcludeFromCodeCoverage]
public abstract partial class VectorizedDataCommands : TypeCollectionBase<VectorizedDataCommandBase, IVectorizedDataCommand>
{
}
