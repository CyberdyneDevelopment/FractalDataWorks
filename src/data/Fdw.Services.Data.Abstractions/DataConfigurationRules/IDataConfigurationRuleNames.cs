using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>
/// The names a data configuration rule shows next to its ids (FDW-6346): the custodian's and the changer's, and the owner's. A name that cannot be
/// found is a coded failure, never an empty text, so a screen never shows an id as if it were a name.
/// </summary>
public interface IDataConfigurationRuleNames
{
    /// <summary>Gets the names of users, one read for each distinct user.</summary>
    /// <param name="userIds">The users; repeats are read once.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The name of each user by id, or the failure naming the first user who has none.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Users(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>Gets the people who may be a rule's custodian, the holders of <c>datastores:write</c>, with their names.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The name of each holder by id; or why the holders could not be read, or the failure naming the first holder who has no name.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Custodians(CancellationToken cancellationToken = default);

    /// <summary>Gets the names of owners of one kind, one read for each distinct owner.</summary>
    /// <param name="ownerKind">The kind of owner, a name in the DataOptionOwnerKinds collection.</param>
    /// <param name="ownerIds">The owners; repeats are read once.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The name of each owner by id, or the failure naming the first owner that is not found.</returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, string>>> Owners(string ownerKind, IEnumerable<Guid> ownerIds, CancellationToken cancellationToken = default);
}
