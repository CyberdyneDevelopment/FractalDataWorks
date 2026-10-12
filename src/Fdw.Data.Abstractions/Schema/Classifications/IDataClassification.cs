using Fdw.Collections;

namespace Fdw.Data.Abstractions;

/// <summary>
/// How sensitive a piece of data is: one level of the <see cref="DataClassifications"/> collection.
/// </summary>
/// <remarks>
/// Set on a data store, data path, data container, container field, data set, data set field or
/// workspace through its <c>ClassificationId</c>, which holds the level's <c>Id</c>. Separate from field visibility and from visibility
/// groups: classification says how sensitive the data is, not who can find it (FDW-938).
/// </remarks>
public interface IDataClassification : ITypeOption<int, DataClassificationBase>
{
    /// <summary>Gets the level's order, least sensitive first.</summary>
    int Sensitivity { get; }

    /// <summary>Gets whether only the owner and invited members may reach data at this level.</summary>
    bool RequiresInvitation { get; }

    /// <summary>Gets what the level means, as shown to the people choosing it.</summary>
    string Description { get; }
}
