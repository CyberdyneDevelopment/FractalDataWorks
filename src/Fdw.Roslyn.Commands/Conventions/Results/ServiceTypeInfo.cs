namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a service type.
/// </summary>
public sealed class ServiceTypeInfo
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the full type name.
    /// </summary>
    public required string FullName { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets whether this is a collection.
    /// </summary>
    public required bool IsCollection { get; init; }

    /// <summary>
    /// Gets or sets whether the type is sealed.
    /// </summary>
    public required bool IsSealed { get; init; }

    /// <summary>
    /// Gets or sets whether the type has a Register method.
    /// </summary>
    public required bool HasRegisterMethod { get; init; }
}