using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Where a dataverse is in its lifecycle.</summary>
public interface IDataverseStatus : ITypeOption<int, DataverseStatusBase>
{
}
