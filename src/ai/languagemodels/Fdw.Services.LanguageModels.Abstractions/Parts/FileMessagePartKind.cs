using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>A file, such as a document.</summary>
[TypeOption(typeof(MessagePartKinds), "File")]
[ExcludeFromCodeCoverage]
public sealed class FileMessagePartKind : MessagePartKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FileMessagePartKind"/> class.</summary>
    public FileMessagePartKind() : base("File", "Chat")
    {
    }
}
