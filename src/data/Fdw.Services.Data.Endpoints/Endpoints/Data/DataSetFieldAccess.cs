using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// How a generic data set route reaches a data set's fields (FDW-5505): through what the data set's kind states, and never through an assumption that the
/// shared field list is the stored list.
/// </summary>
/// <remarks>
/// A kind that keeps its fields apart states <see cref="IKeepsItsOwnFields"/>; a kind that states nothing keeps the shared list, which is then edited in place as
/// it always was. No kind is named here.
/// </remarks>
internal static class DataSetFieldAccess
{
    /// <summary>Gets what a kind states about where it keeps its fields, or null when it keeps the shared list.</summary>
    /// <param name="implementation">The data set's kind.</param>
    /// <returns>The statement, or null.</returns>
    internal static IKeepsItsOwnFields? Kept(string implementation) => DataSetTypes.ByName(implementation) as IKeepsItsOwnFields;

    /// <summary>Gets the list a route edits: the shared list itself for a kind that keeps it, and the kind's own fields for one that does not.</summary>
    /// <param name="dataSet">The data set, read with its children.</param>
    /// <returns>The fields; after an edit, <see cref="Keep"/> puts them back.</returns>
    internal static IList<DataSetFieldConfiguration> Of(IDataSetImplementationConfiguration dataSet) =>
        Kept(dataSet.Implementation) is { } keeping ? [.. keeping.FieldsOf(dataSet)] : dataSet.Fields;

    /// <summary>Puts a list a route edited back, on the data set about to be saved; nothing to do for a kind that keeps the shared list.</summary>
    /// <param name="dataSet">The data set about to be saved.</param>
    /// <param name="fields">The list <see cref="Of"/> gave, as edited.</param>
    internal static void Keep(IDataSetImplementationConfiguration dataSet, IList<DataSetFieldConfiguration> fields)
    {
        if (Kept(dataSet.Implementation) is { } keeping)
        {
            keeping.KeepFields(dataSet, [.. fields]);
        }
    }

    /// <summary>Refuses a route that reads the versions a field has been through, when the data set's kind keeps no such versions (FDW-5505).</summary>
    /// <param name="dataSet">The data set the route is about.</param>
    /// <param name="field">The field the route names.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when the kind keeps field versions or states nothing about them; otherwise <c>DataSetFieldHistoryNotKept</c>.</returns>
    internal static IGenericResult AllowHistory(IDataSetImplementationConfiguration dataSet, string field, ILogger logger) =>
        Kept(dataSet.Implementation) is { FieldsHaveHistory: false }
            ? GenericResult.Failure(DataServiceResultCodes.ByName("DataSetFieldHistoryNotKept"), logger, ResultDetails.Create("dataSet", dataSet.Name, "field", field))
            : GenericResult.Success();

    /// <summary>Refuses a route that sets, reads, clears or previews a stand-in or a sample on a field, when the data set's kind takes none (FDW-5505).</summary>
    /// <param name="dataSet">The data set the route is about.</param>
    /// <param name="field">The field the route names.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when the kind takes stand-ins or states nothing about them; otherwise <c>DataSetFieldStandInsNotTaken</c>.</returns>
    internal static IGenericResult AllowStandIns(IDataSetImplementationConfiguration dataSet, string field, ILogger logger) =>
        Kept(dataSet.Implementation) is { FieldsTakeStandIns: false }
            ? GenericResult.Failure(DataServiceResultCodes.ByName("DataSetFieldStandInsNotTaken"), logger, ResultDetails.Create("dataSet", dataSet.Name, "field", field))
            : GenericResult.Success();

    /// <summary>Refuses a route about the stand-ins or samples of a whole data set, which names no field, when the data set's kind takes none (FDW-5505).</summary>
    /// <param name="dataSet">The data set the route is about.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>Success when the kind takes stand-ins or states nothing about them; otherwise <c>DataSetStandInsNotTaken</c>.</returns>
    internal static IGenericResult AllowStandIns(IDataSetImplementationConfiguration dataSet, ILogger logger) =>
        Kept(dataSet.Implementation) is { FieldsTakeStandIns: false }
            ? GenericResult.Failure(DataServiceResultCodes.ByName("DataSetStandInsNotTaken"), logger, ResultDetails.Create("dataSet", dataSet.Name))
            : GenericResult.Success();
}
