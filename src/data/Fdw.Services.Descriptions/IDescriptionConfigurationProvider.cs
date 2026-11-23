using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Descriptions.Configuration;

namespace Fdw.Services.Descriptions;

/// <summary>Supplies the configured descriptions: a member whole, or a page of them as domain records.</summary>
public interface IDescriptionConfigurationProvider
    : IDomainConfigurationProvider<IDescriptionImplementationConfiguration>,
      IDomainRecordProvider<DescriptionDomainConfiguration>
{
    /// <summary>Saves a description into a unit of work the caller opened and will commit (FDW-6121).</summary>
    /// <typeparam name="T">The implementation contract being saved.</typeparam>
    /// <param name="implementationConfiguration">The description to write.</param>
    /// <param name="domain">The domain the description belongs to.</param>
    /// <param name="implementationName">The implementation that holds it.</param>
    /// <param name="name">The description's name.</param>
    /// <param name="unit">The unit the write runs in.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or the failure that stopped the write.</returns>
    Task<IGenericResult> Save<T>(
        T implementationConfiguration, string domain, string implementationName, string name, ConfigurationWriteScope unit, CancellationToken ct = default)
        where T : IDescriptionImplementationConfiguration;

    /// <summary>Deletes a description inside a unit of work the caller opened and will commit (FDW-6121).</summary>
    /// <param name="id">The description's durable Id.</param>
    /// <param name="unit">The unit the delete runs in.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success, or the failure that stopped the delete.</returns>
    Task<IGenericResult> Delete(Guid id, ConfigurationWriteScope unit, CancellationToken ct = default);

    /// <summary>Runs work in one unit of work, which is committed when the work succeeds and rolled back when it fails (FDW-6123).</summary>
    /// <param name="work">What to write, given the open unit.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>Success once the unit is committed, or the failure of the work or of the commit.</returns>
    /// <remarks>A description saved or deleted outside a unit of a caller's is several rows when it is several parts, and they land together or not at all.</remarks>
    Task<IGenericResult> InUnit(Func<ConfigurationWriteScope, Task<IGenericResult>> work, CancellationToken ct = default);

    /// <summary>Reads the descriptions a filter selects inside a unit, as the unit sees them, so a save sees the parts it is about to replace (FDW-6123).</summary>
    /// <param name="filter">Which rows; the caller states the version flags it wants.</param>
    /// <param name="unit">The unit the read runs in.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>The rows, or the failure of the read.</returns>
    Task<IGenericResult<IReadOnlyList<DescriptionDomainConfiguration>>> ReadCurrent(FilterExpression filter, ConfigurationWriteScope unit, CancellationToken ct = default);
}
