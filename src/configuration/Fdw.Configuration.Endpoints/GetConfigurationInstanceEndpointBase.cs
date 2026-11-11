using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Configuration.Endpoints.Logging;
using Fdw.Data.Abstractions;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace Fdw.Configuration.Endpoints;

/// <summary>
/// Base endpoint for getting a specific configuration instance with all values.
/// </summary>
/// <remarks>
/// Only containers that hold configurations of their own are read: a container whose rows hang from another container's rows (an
/// implementation body, a typed child list, a key/value property collection) is refused (FDW-4644). A value is masked when the owning
/// configuration type declares its member <see cref="SecretValueAttribute"/>, and by nothing else: a column whose name only looks secret
/// is not masked by its name (FDW-3002). The owning type comes from <see cref="ConfigurationTypeOf"/>; a container whose type is not
/// declared answers <c>InstanceTypeNotDeclared</c> and none of its values is returned.
/// </remarks>
public abstract class GetConfigurationInstanceEndpointBase : Endpoint<GetConfigurationInstanceRequest, ConfigurationInstanceDetailResponse>
{
    private const string SecretMask = "********";

    private static readonly HashSet<string> SystemFields = new(StringComparer.OrdinalIgnoreCase)
        { "Id", "Name", "Type", "ServiceType", "CreatedAt", "ModifiedAt" };

    private readonly IConfigurationGatewayProvider _configurationGateways;

    private static string PlatformConfigurationConnection => PlatformConnection.Name;

    // The platform's own rows are read through the configuration gateway onto that store, never the request-facing
    // Main gateway (FDW-1068): Main runs whatever data store a request names, and these rows are not a request's to name.
    private IDataGateway Gateway => _configurationGateways.Get(PlatformConfigurationConnection) is { IsSuccess: true, Value: { } gateway }
        ? gateway
        : throw new InvalidOperationException(
            $"No configuration gateway is available for connection '{PlatformConfigurationConnection}'. It must be declared in the host's configurationSchema.json.");
    private readonly IConfigurationContainerLookup _containerLookup;
    private readonly ILogger<GetConfigurationInstanceEndpointBase> _logger;

    /// <summary>Initializes a new instance of the endpoint.</summary>
    protected GetConfigurationInstanceEndpointBase(
        IConfigurationGatewayProvider configurationGateways,
        IConfigurationContainerLookup containerLookup,
        ILogger<GetConfigurationInstanceEndpointBase> logger)
    {
        _configurationGateways = configurationGateways;
        _containerLookup = containerLookup;
        _logger = logger ?? NullLogger<GetConfigurationInstanceEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/configuration/instances/{Category}/{Name}");
        Policies("configurations:read");
        Summary(s =>
        {
            s.Summary = "Get configuration instance details";
            s.Description = "Returns detailed configuration values for a specific instance.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetConfigurationInstanceRequest req, CancellationToken ct)
    {
        ConfigurationEndpointLog.GettingInstance(_logger, req.Category, req.Name);

        var containers = _containerLookup.ByCategory(req.Category);
        if (containers.Count == 0)
        {
            ConfigurationEndpointLog.NoCategoryTypes(_logger, req.Category);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.GetConfigurationInstanceNotFound(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var topLevel = TopLevelConfigurationContainers.Of(containers, _logger);
        if (topLevel.Count == 0)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.CategoryHasNoTopLevelConfiguration(_logger, req.Category)), HttpContext)).ConfigureAwait(false);
            return;
        }

        IGenericResult<ConfigurationInstanceDetailResponse?> found;
        try
        {
            found = await FindInstanceDetail(req, topLevel, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            ConfigurationEndpointLog.TableQueryFailed(_logger, ex, req.Category, $"instance {req.Name}");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.GetConfigurationInstanceFaulted(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (found.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(found, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (found.Value is { } detail)
        {
            await Send.OkAsync(detail, ct).ConfigureAwait(false);
            return;
        }

        ConfigurationEndpointLog.InstanceNotFound(_logger, req.Name, req.Category);
        await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConfigurationEndpointLog.GetConfigurationInstanceNotFound2(_logger)), HttpContext)).ConfigureAwait(false);
    }

    /// <summary>The configuration type that owns the rows of <paramref name="container"/>, whose secret members are masked.</summary>
    /// <param name="container">A container that holds configurations of its own.</param>
    /// <returns>The owning type, or a failure when the container does not declare one.</returns>
    /// <remarks>
    /// A data container does not yet carry the configuration type it stores, so no container declares one here and every container
    /// answers <c>InstanceTypeNotDeclared</c>: without the type nothing says which of its values are secret, so none is returned.
    /// A derived endpoint that knows a container's type returns it.
    /// </remarks>
    protected virtual IGenericResult<Type> ConfigurationTypeOf(IDataContainer container)
        => GenericResult<Type>.Failure(ConfigurationEndpointLog.InstanceTypeNotDeclared(_logger, container.Parent.Name, container.Name));

    private async Task<IGenericResult<ConfigurationInstanceDetailResponse?>> FindInstanceDetail(
        GetConfigurationInstanceRequest req,
        List<IDataContainer> containers,
        CancellationToken ct)
    {
        foreach (var container in containers)
        {
            var command = DataQuery.From<Dictionary<string, object?>>(PlatformConfigurationConnection, container.Parent.Name, container.Name)
                .Where("Name", req.Name)
                .Build();

            try
            {
                var result = await Gateway.Execute<IEnumerable<Dictionary<string, object?>>>(command, ct)
                    .ConfigureAwait(false);

                if (!result.IsSuccess || result.Value?.FirstOrDefault() is not { } record)
                {
                    continue;
                }

                var owner = ConfigurationTypeOf(container);
                if (owner.IsFailure)
                    return owner.ToNewResult<ConfigurationInstanceDetailResponse?>();
                if (owner.Value is not { } ownerType)
                    return GenericResult<ConfigurationInstanceDetailResponse?>.Failure(
                        ConfigurationEndpointLog.InstanceTypeNotDeclared(_logger, container.Parent.Name, container.Name));

                return GenericResult<ConfigurationInstanceDetailResponse?>.Success(
                    MapToDetail(record, container, ownerType, req.Name, req.Category));
            }
            catch (Exception ex)
            {
                ConfigurationEndpointLog.TableQueryFailed(_logger, ex, container.Name, $"instance {req.Name}");
                throw;
            }
        }

        return GenericResult<ConfigurationInstanceDetailResponse?>.Success(null);
    }

    private ConfigurationInstanceDetailResponse MapToDetail(
        Dictionary<string, object?> record,
        IDataContainer container,
        Type ownerType,
        string requestedName,
        string category)
    {
        // Every stored instance has an Id, a Name and a CreatedAt. A row without one is broken data: it fails here,
        // and is never returned with an invented value standing in for the missing column.
        if (!record.TryGetValue("Id", out var idValue) || idValue is not Guid id)
            throw RowIncomplete(container, requestedName, "Id");

        if (!record.TryGetValue("Name", out var nameValue) || nameValue?.ToString() is not { } name)
            throw RowIncomplete(container, requestedName, "Name");

        if (!record.TryGetValue("CreatedAt", out var createdValue) || createdValue is not DateTime createdAt)
            throw RowIncomplete(container, requestedName, "CreatedAt");

        var serviceType = record.TryGetValue("Type", out var typeValue)
            ? typeValue?.ToString()
            : null;
        var modifiedAt = record.TryGetValue("ModifiedAt", out var modifiedValue) && modifiedValue is DateTime modified
            ? (DateTime?)modified : null;

        return new ConfigurationInstanceDetailResponse
        {
            Id = id,
            Name = name,
            ServiceType = serviceType,
            Category = category,
            Values = ExtractValues(record, SecretMembers(ownerType)),
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt
        };
    }

    private InvalidOperationException RowIncomplete(IDataContainer container, string requestedName, string column) =>
        new(ConfigurationEndpointLog.InstanceRowIncomplete(_logger, requestedName, container.Name, column).Message);

    /// <summary>The row's values with each value of a declared secret member masked.</summary>
    /// <param name="record">The stored row.</param>
    /// <param name="secretMembers">The names of the owning type's members that carry <see cref="SecretValueAttribute"/>.</param>
    /// <returns>The values, system fields left out.</returns>
    private static Dictionary<string, object?> ExtractValues(Dictionary<string, object?> record, HashSet<string> secretMembers)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in record)
        {
            if (SystemFields.Contains(kvp.Key))
            {
                continue;
            }

            values[kvp.Key] = secretMembers.Contains(kvp.Key) && kvp.Value is not null ? SecretMask : kvp.Value;
        }

        return values;
    }

    /// <summary>The names of the public instance properties of <paramref name="ownerType"/> that carry <see cref="SecretValueAttribute"/>.</summary>
    /// <param name="ownerType">The owning configuration type.</param>
    /// <returns>The member names, compared ignoring case as the row's columns are.</returns>
    /// <remarks>Whether a member is secret is decided by the attribute alone, inherited from a base class too, never by its name (FDW-3002).</remarks>
    private static HashSet<string> SecretMembers(Type ownerType)
        => new HashSet<string>(
            ownerType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => Attribute.IsDefined(property, typeof(SecretValueAttribute), inherit: true))
                .Select(property => property.Name),
            StringComparer.OrdinalIgnoreCase);
}
