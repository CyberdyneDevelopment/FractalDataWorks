using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// What a kind of data set states when it keeps its fields apart from the shared field list (FDW-5505): where they live, how an edited list is put back, and
/// which of the things that hang from a row of the shared field table it has.
/// </summary>
/// <remarks>
/// <para>
/// The shared list (<see cref="IDataSetImplementationConfiguration.Fields"/>) is the stored list for Simple, Compound and Federated, and a route that edits a field and
/// saves the data set changes what is stored. A kind that keeps its fields elsewhere (a Graph data set keeps them in its own table, and its shared list is a copy)
/// states this capability, so a generic route reads and writes its fields through the kind and not through an assumption about the shared list. A kind that states
/// nothing keeps the shared list, and every route behaves as it did.
/// </para>
/// <para>
/// A route above the kind layer asks <c>DataSetTypes.ByName(implementation) is IKeepsItsOwnFields</c> and never names a kind.
/// </para>
/// </remarks>
public interface IKeepsItsOwnFields
{
    /// <summary>Gets whether a field of the kind has a version history in the shared field table.</summary>
    bool FieldsHaveHistory { get; }

    /// <summary>Gets whether a stand-in or a sample set on a field of the kind feeds what a read of the data set returns.</summary>
    bool FieldsTakeStandIns { get; }

    /// <summary>Reads the data set's fields as the shared type from where the kind keeps them.</summary>
    /// <param name="dataSet">The data set, read with its children.</param>
    /// <returns>The fields; a change to a member of one is put back with <see cref="KeepFields"/>.</returns>
    IReadOnlyList<DataSetFieldConfiguration> FieldsOf(IDataSetImplementationConfiguration dataSet);

    /// <summary>Puts edited fields back where the kind keeps them, on the data set that is about to be saved.</summary>
    /// <param name="dataSet">The data set about to be saved.</param>
    /// <param name="fields">The fields as edited, all of them.</param>
    void KeepFields(IDataSetImplementationConfiguration dataSet, IReadOnlyList<DataSetFieldConfiguration> fields);
}
