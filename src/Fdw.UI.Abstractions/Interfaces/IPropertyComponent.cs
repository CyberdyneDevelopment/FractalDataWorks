namespace Fdw.UI.Abstractions;

/// <summary>
/// Non-generic interface for property components.
/// Allows heterogeneous collections of property components.
/// </summary>
public interface IPropertyComponent
{
    PropertyMetadata? Metadata { get; set; }
    bool ReadOnly { get; set; }
}
