namespace Fdw.CodeBuilder.Analysis;

/// <summary>
/// Interface for field expectations.
/// </summary>
public interface IFieldExpectations
{
    /// <summary>
    /// Expects the field to have a specific type.
    /// </summary>
    /// <param name="fieldType">The expected field type.</param>
    /// <returns>The expectations instance for chaining.</returns>
    IFieldExpectations HasType(string fieldType);

    /// <summary>
    /// Expects the field to be readonly.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IFieldExpectations IsReadOnly();

    /// <summary>
    /// Expects the field to be static.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IFieldExpectations IsStatic();

    /// <summary>
    /// Expects the field to be const.
    /// </summary>
    /// <returns>The expectations instance for chaining.</returns>
    IFieldExpectations IsConst();
}
