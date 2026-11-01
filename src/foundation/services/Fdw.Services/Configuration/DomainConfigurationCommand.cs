using Fdw.Configuration;

namespace Fdw.Services.Configuration;

/// <summary>
/// The commands over a domain table.
/// </summary>
/// <typeparam name="TDomain">
/// The domain's record: <see cref="DomainConfiguration"/> for a domain that shows no fields in its
/// list, or a record derived from it that adds the fields its list shows.
/// </typeparam>
/// <remarks>
/// One class for every domain. A domain table is the same four columns whatever it is called, plus at
/// most the fields its list shows, which the record type declares, so the only thing that varies is
/// the name -- which is an argument, not a type. Constructed by the provider with the table it was told
/// to read, rather than resolved as a TypeOption: a singleton could carry one table name, and there
/// are thirty-one of them.
/// </remarks>
public sealed class DomainConfigurationCommand<TDomain> : ConfigurationCommandBase<TDomain>
    where TDomain : DomainConfiguration
{
    /// <summary>Initializes a new instance of the <see cref="DomainConfigurationCommand{TDomain}"/> class.</summary>
    /// <param name="tableName">The domain table this reads and writes.</param>
    public DomainConfigurationCommand(string tableName) : base(tableName)
    {
    }
}
