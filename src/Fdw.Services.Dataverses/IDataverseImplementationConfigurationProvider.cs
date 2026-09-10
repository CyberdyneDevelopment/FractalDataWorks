using Fdw.Services.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>Supplies the Dataverse implementation's own configuration to the domain that registers it.</summary>
public interface IDataverseImplementationConfigurationProvider
    : IImplementationConfigurationProvider<IDataverseImplementationConfiguration>
{
}
