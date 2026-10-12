using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces;

/// <summary>
/// What the calling user may read through workspaces: which workspaces, which data sets each one
/// exposes, and the one data set a name reaches.
/// </summary>
/// <remarks>
/// <para>
/// The workspace is the grant a third party's credential is meant for (FDW-876): membership reaches
/// every data set attached to it and nothing else (FDW-937). Every way of reading through one — the
/// HTTP query route, a SQL Server connection — asks here, so none can reach a data set another would
/// refuse.
/// </para>
/// <para>
/// Nothing outside the grant is loaded or disclosed. A workspace that does not exist and one the caller
/// may not read answer the same refusal, <c>WorkspaceNotReadable</c>; a data set is found by walking the
/// workspace's own attachments, so one outside it answers <c>WorkspaceDataSetNotAttached</c> whether or
/// not it exists elsewhere.
/// </para>
/// </remarks>
public interface IWorkspaceReadService
{
    /// <summary>Gets every workspace the calling user may read.</summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The readable workspaces, or the failure reading them.</returns>
    Task<IGenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>> Readable(
        CancellationToken cancellationToken = default);

    /// <summary>Gets one workspace by name, when the calling user may read it.</summary>
    /// <param name="workspaceName">The workspace's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The workspace, or <c>WorkspaceNotReadable</c> whether it is absent or refused.</returns>
    Task<IGenericResult<IWorkspaceImplementationConfiguration>> Readable(
        string workspaceName, CancellationToken cancellationToken = default);

    /// <summary>Gets every data set attached to a workspace the caller may read.</summary>
    /// <param name="workspace">A workspace <see cref="Readable(string, CancellationToken)"/> answered.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The attached data sets, each with the data set its query runs.</returns>
    /// <remarks>
    /// An attachment that cannot be loaded is logged and left out, so one broken data set does not hide
    /// every other one; asking for it by name reports its own failure.
    /// </remarks>
    Task<IGenericResult<IReadOnlyList<ReadableDataSet>>> DataSets(
        IWorkspaceImplementationConfiguration workspace, CancellationToken cancellationToken = default);

    /// <summary>Gets the data set a name reaches through a workspace the caller may read.</summary>
    /// <param name="workspaceName">The workspace's name.</param>
    /// <param name="dataSetName">The name the data set is attached under.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The data set and the data set its query runs, or the refusal.</returns>
    Task<IGenericResult<ReadableDataSet>> DataSet(
        string workspaceName, string dataSetName, CancellationToken cancellationToken = default);
}
