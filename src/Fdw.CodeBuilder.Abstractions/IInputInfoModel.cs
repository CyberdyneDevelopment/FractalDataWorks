using System.IO;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Interface for models that support input change tracking for incremental generation.
/// Used by code generators to determine when regeneration is needed.
/// </summary>
public interface IInputInfoModel
{
    /// <summary>
    /// Gets a hash representing the current state of the model.
    /// </summary>
    string InputHash { get; }

    /// <summary>
    /// Writes the model state to a TextWriter for hash calculation.
    /// </summary>
    /// <param name="writer">The text writer to write to.</param>
    void WriteToHash(TextWriter writer);
}
