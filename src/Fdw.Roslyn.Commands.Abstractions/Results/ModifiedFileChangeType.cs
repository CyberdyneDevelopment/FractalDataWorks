using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The file was modified.</summary>
[TypeOption(typeof(FileChangeTypes), "Modified")]
[ExcludeFromCodeCoverage]
public sealed class ModifiedFileChangeType : FileChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ModifiedFileChangeType"/>.</summary>
    public ModifiedFileChangeType() : base(2, "Modified") { }
}
