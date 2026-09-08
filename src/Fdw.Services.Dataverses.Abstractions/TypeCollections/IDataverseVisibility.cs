using Fdw.Collections;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Who can find a dataverse.</summary>
public interface IDataverseVisibility : ITypeOption<int, DataverseVisibilityBase>
{
}
