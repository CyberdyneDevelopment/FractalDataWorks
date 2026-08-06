using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Base class for file change types.
/// </summary>
public abstract class FileChangeTypeBase : TypeOptionBase<int, FileChangeTypeBase>, IFileChangeType
{
    /// <summary>
    /// Initializes a new instance of <see cref="FileChangeTypeBase"/>.
    /// </summary>
    protected FileChangeTypeBase(int id, string name) : base(id, name) { }
}
