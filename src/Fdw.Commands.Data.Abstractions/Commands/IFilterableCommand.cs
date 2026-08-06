using Fdw.Data.Abstractions;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Interface for commands that support filtering via WHERE clause.
/// Provides strongly-typed access to Filter property without reflection.
/// </summary>
/// <remarks>
/// Implemented by UpdateCommand, DeleteCommand, and other commands that filter records.
/// Enables translators to access the Filter property without reflection.
/// </remarks>
public interface IFilterableCommand : IDataCommand
{
    /// <summary>
    /// Gets the filter expression (WHERE clause).
    /// </summary>
    IFilterExpression? Filter { get; }
}
