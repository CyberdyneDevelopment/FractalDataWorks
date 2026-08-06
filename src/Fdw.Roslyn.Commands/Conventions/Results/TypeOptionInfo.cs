namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a type option.
/// </summary>
public sealed class TypeOptionInfo
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the option name.
    /// </summary>
    public required string OptionName { get; init; }

    /// <summary>
    /// Gets or sets the full type name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets or sets the collection type.
    /// </summary>
    public required string Collection { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets whether the type is sealed.
    /// </summary>
    public required bool IsSealed { get; init; }
}