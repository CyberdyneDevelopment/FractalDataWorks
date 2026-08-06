using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Formatting.Commands;
/// <summary>
/// Command to apply naming conventions to code symbols.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ApplyNamingConventions")]
public sealed class ApplyNamingConventionsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyNamingConventionsCommand"/> class.
    /// </summary>
    public ApplyNamingConventionsCommand()
        : base("ApplyNamingConventions", RoslynCommandCategories.Formatting, "Rewrite symbol names that violate the configured naming conventions (PascalCase types, _camelCase private fields, etc.). Use to normalize a codebase before enforcing the convention in CI. Returns the renames performed with old and new names.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prefix for private fields.
    /// </summary>
    public string PrivateFieldPrefix { get; set; } = "_";
    /// <summary>
    /// Gets or sets whether to add Async suffix to async methods.
    /// </summary>
    public bool UseAsyncSuffix { get; set; }
}
