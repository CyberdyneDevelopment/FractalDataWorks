using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Supplies the configured data set variations: a member whole, or a page of them as list records.</summary>
/// <remarks>
/// A variation is a data set of kind Variation (FDW-3638), stored in <c>data.DataSet</c> and returned by
/// <see cref="IDataSetConfigurationProvider"/> with the rest. This is the view of the data sets that are variations: a member read
/// answers nothing for a data set of another kind, and a write saves the data set through the data set provider. The reads that decide
/// something about a variation (which data set it varies, which workspace owns it) are list-record reads: one query, no variation
/// composed.
/// </remarks>
public interface IDataSetVariationConfigurationProvider : IDomainRecordProvider<DataSetVariationDomainConfiguration>
{
    /// <summary>Reads the variation with this name.</summary>
    /// <param name="name">The data set's name.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The variation; success with nothing when no variation has the name, including when a data set of another kind has it.</returns>
    Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(string name, CancellationToken ct = default);

    /// <summary>Reads the variation with this name, to a given depth.</summary>
    /// <param name="name">The data set's name.</param>
    /// <param name="levels">How deep to read: a <c>ConfigurationLevels</c> value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>As the overload without a depth.</returns>
    Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(string name, int levels, CancellationToken ct = default);

    /// <summary>Reads the variation with this id.</summary>
    /// <param name="id">The data set's logical id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The variation; success with nothing when no variation has the id, including when a data set of another kind has it.</returns>
    Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(Guid id, CancellationToken ct = default);

    /// <summary>Reads the variation with this id, to a given depth.</summary>
    /// <param name="id">The data set's logical id.</param>
    /// <param name="levels">How deep to read: a <c>ConfigurationLevels</c> value.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>As the overload without a depth.</returns>
    Task<IGenericResult<IDataSetVariationImplementationConfiguration?>> Get(Guid id, int levels, CancellationToken ct = default);

    /// <summary>Saves a variation, creating it or replacing the one with this name.</summary>
    /// <param name="variation">The variation.</param>
    /// <param name="name">The data set's name.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IGenericResult> Save(IDataSetVariationImplementationConfiguration variation, string name, CancellationToken ct = default);

    /// <summary>Updates the variation with this name; it must exist.</summary>
    /// <param name="variation">The variation.</param>
    /// <param name="name">The data set's name.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IGenericResult> Update(IDataSetVariationImplementationConfiguration variation, string name, CancellationToken ct = default);

    /// <summary>Retires the variation with this id.</summary>
    /// <param name="id">The data set's logical id.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IGenericResult> Delete(Guid id, CancellationToken ct = default);
}
