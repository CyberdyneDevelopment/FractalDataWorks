using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// TypeCollection for file change types.
/// </summary>
[TypeCollection(typeof(FileChangeTypeBase), typeof(IFileChangeType), typeof(FileChangeTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class FileChangeTypes : TypeCollectionBase<FileChangeTypeBase, IFileChangeType> { }
