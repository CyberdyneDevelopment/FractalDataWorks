using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The file was removed.</summary>
[TypeOption(typeof(FileChangeTypes), "Removed")]
[ExcludeFromCodeCoverage]
public sealed class RemovedFileChangeType : FileChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="RemovedFileChangeType"/>.</summary>
    public RemovedFileChangeType() : base(3, "Removed") { }
}
