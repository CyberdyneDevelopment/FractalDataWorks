using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>The file was added.</summary>
[TypeOption(typeof(FileChangeTypes), "Added")]
[ExcludeFromCodeCoverage]
public sealed class AddedFileChangeType : FileChangeTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AddedFileChangeType"/>.</summary>
    public AddedFileChangeType() : base(1, "Added") { }
}
