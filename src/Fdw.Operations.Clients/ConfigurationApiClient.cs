namespace Fdw.Operations.Clients;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Clients.Models;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;

/// <summary>
/// API client for configuration type discovery and management.
/// </summary>
public class ConfigurationApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationApiClient"/> class.
    /// </summary>
    public ConfigurationApiClient(HttpClient httpClient, ILogger<ConfigurationApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all configuration types for a service category.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of configuration type summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConfigurationTypeSummary>>> GetTypesByCategory(string category, CancellationToken ct = default)
        => GetList<ConfigurationTypeSummary>($"configuration/types?category={Uri.EscapeDataString(category)}", ct);

    /// <summary>
    /// Gets detailed information for a specific configuration type.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="serviceType">The service type name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing detailed information about the configuration type.</returns>
    public virtual Task<IGenericResult<ConfigurationTypeDetail>> GetTypeDetail(string category, string serviceType, CancellationToken ct = default)
        => Get<ConfigurationTypeDetail>($"configuration/types/detail?category={Uri.EscapeDataString(category)}&type={Uri.EscapeDataString(serviceType)}", ct);

    /// <summary>
    /// Gets all root configuration types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of root configuration type summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConfigurationTypeSummary>>> GetRootTypes(CancellationToken ct = default)
        => GetList<ConfigurationTypeSummary>("configuration/types/roots", ct);

    /// <summary>
    /// Gets child configuration types for a parent table.
    /// </summary>
    /// <param name="parentTableName">The parent table name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of child configuration type summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConfigurationTypeSummary>>> GetChildTypes(string parentTableName, CancellationToken ct = default)
        => GetList<ConfigurationTypeSummary>($"configuration/types/children?parent={Uri.EscapeDataString(parentTableName)}", ct);

    /// <summary>
    /// Gets all configuration instances, optionally filtered by category.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of configuration instance summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConfigurationInstanceSummaryPayload>>> GetInstances(string? category = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrEmpty(category)
            ? "configuration/instances"
            : $"configuration/instances?category={Uri.EscapeDataString(category!)}";

        return GetList<ConfigurationInstanceSummaryPayload>(url, ct);
    }

    /// <summary>
    /// Gets a specific configuration instance with all values.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="name">The instance name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the configuration instance detail.</returns>
    public virtual Task<IGenericResult<ConfigurationInstanceDetailPayload>> GetInstance(string category, string name, CancellationToken ct = default)
        => Get<ConfigurationInstanceDetailPayload>($"configuration/instances/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(name)}", ct);

    /// <summary>
    /// Creates a new configuration instance.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created instance detail.</returns>
    public virtual Task<IGenericResult<ConfigurationInstanceDetailPayload>> CreateInstance(string category, CreateConfigurationInstanceRequest request, CancellationToken ct = default)
        => Post<CreateConfigurationInstanceRequest, ConfigurationInstanceDetailPayload>($"configuration/instances/{Uri.EscapeDataString(category)}", request, ct);

    /// <summary>
    /// Updates an existing configuration instance.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="name">The instance name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated instance detail.</returns>
    public virtual Task<IGenericResult<ConfigurationInstanceDetailPayload>> UpdateInstance(string category, string name, UpdateConfigurationInstanceRequest request, CancellationToken ct = default)
        => Put<UpdateConfigurationInstanceRequest, ConfigurationInstanceDetailPayload>($"configuration/instances/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(name)}", request, ct);

    /// <summary>
    /// Deletes a configuration instance.
    /// </summary>
    /// <param name="category">The category name.</param>
    /// <param name="name">The instance name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion was successful.</returns>
    public virtual Task<IGenericResult> DeleteInstance(string category, string name, CancellationToken ct = default)
        => Delete($"configuration/instances/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(name)}", ct);

    /// <summary>
    /// Gets all TypeOption values for a named TypeCollection.
    /// </summary>
    /// <param name="collectionName">The TypeCollection name (e.g., "ConnectionTypes").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of TypeCollection value summaries.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<TypeCollectionValueSummary>>> GetTypeCollectionValues(string collectionName, CancellationToken ct = default)
        => GetList<TypeCollectionValueSummary>($"type-collections/{Uri.EscapeDataString(collectionName)}/values", ct);
}
