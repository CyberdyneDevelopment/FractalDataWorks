using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>What kind of category a node is (FDW-1465): its implementation. The type says what the category is for and makes the stored form of it.</summary>
public interface ICategoryType : ITypeOption<int, ICategoryType>
{
    /// <summary>Gets what a category of this type is for.</summary>
    string Description { get; }

    /// <summary>Makes a new, empty category of this type, which a create then fills.</summary>
    /// <returns>The category.</returns>
    ICategoryImplementationConfiguration NewConfiguration();
}
