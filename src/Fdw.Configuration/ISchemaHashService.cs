namespace Fdw.Configuration;

/// <summary>
/// Interface for calculating schema hashes for change detection.
/// </summary>
public interface ISchemaHashService
{
    /// <summary>
    /// Calculates a hash for a schema object.
    /// </summary>
    /// <typeparam name="T">The schema type.</typeparam>
    /// <param name="schema">The schema to hash.</param>
    /// <returns>A hash string representing the schema.</returns>
    string CalculateHash<T>(T schema) where T : class;
}
