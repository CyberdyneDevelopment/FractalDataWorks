using Fdw.Results;

namespace Fdw.Validation.Abstractions;

/// <summary>
/// Defines a validator for an entity type. Allows Abstractions packages
/// (targeting netstandard2.0) to declare validation contracts without
/// depending on FluentValidation.
/// </summary>
/// <typeparam name="T">The entity type to validate.</typeparam>
public interface IEntityValidator<in T>
{
    /// <summary>
    /// Validates the specified entity and returns a result indicating success or failure.
    /// </summary>
    /// <param name="entity">The entity to validate.</param>
    /// <returns>A <see cref="IGenericResult"/> indicating success or containing validation errors.</returns>
    IGenericResult Validate(T entity);
}
