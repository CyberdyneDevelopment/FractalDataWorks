using Fdw.Configuration;

namespace Fdw.Services.Configuration;

/// <summary>
/// The commands over an implementation table.
/// </summary>
/// <typeparam name="TConfiguration">The implementation this reads and writes.</typeparam>
/// <remarks>
/// The counterpart to <see cref="DomainConfigurationCommand"/>, and the pair is a real
/// distinction rather than symmetry: an implementation row is always reached by joining to its
/// domain on RowId, and a domain row never is. That was once a runtime check on the row's shape;
/// as two types it cannot be branched on.
/// <para>
/// One class for every implementation. The configuration type selects the mapper and the name
/// selects the table, so nothing else varies -- which is why the 130 hand-written commands it
/// replaces were each a constructor and nothing else.
/// </para>
/// </remarks>
public sealed class ImplementationConfigurationCommand<TConfiguration>
    : ConfigurationCommandBase<TConfiguration>
    where TConfiguration : class, IImplementationConfiguration
{
    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="tableName">The implementation table this reads and writes.</param>
    public ImplementationConfigurationCommand(string tableName) : base(tableName)
    {
    }
}
