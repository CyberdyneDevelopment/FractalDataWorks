namespace Fdw.Data.Serialization;

/// <summary>
/// Empty container properties for containers with no additional properties.
/// </summary>
public sealed class EmptyContainerProperties : IContainerProperties
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static EmptyContainerProperties Instance { get; } = new();

    private EmptyContainerProperties() { }
}