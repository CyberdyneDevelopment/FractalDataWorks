using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Dataverses.Abstractions;
using Fdw.Services.Dataverses.Commands;
using Fdw.Services.Dataverses.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Dataverses;

/// <summary>
/// Reads and writes <see cref="DataverseConfiguration"/> and its children.
/// </summary>
/// <remarks>
/// There are no Get overrides here. The base composes the aggregate — members, resources and
/// relationships come back populated — because those are direct children of the dataverse row.
/// <c>DataSetConfigurationProvider</c> overrides Get only to reach a grandchild, which a dataverse
/// does not have.
/// </remarks>
public class DataverseConfigurationProvider
    : ImplementationConfigurationProviderBase<IDataverseImplementationConfiguration>,
      IDataverseConfigurationProvider
{

    /// <summary>
    /// Registers the provider and the interfaces callers resolve it through.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<DataverseConfigurationProvider>(sp =>
            new DataverseConfigurationProvider(
                sp.GetService<ILogger<DataverseConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection,
                "dataverse"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<IDataverseImplementationConfiguration>>(
            sp => sp.GetRequiredService<DataverseConfigurationProvider>());

        services.TryAddSingleton<IDataverseConfigurationProvider>(
            sp => sp.GetRequiredService<DataverseConfigurationProvider>());

        // Scoped, unlike the providers above: it reads the calling user off the ambient
        // authentication context, so it answers per request rather than once per process.
        services.TryAddScoped<IDataverseAccessPolicy>(sp =>
            new DataverseAccessPolicy(
                sp.GetRequiredService<IAuthenticationContextAccessor>(),
                sp.GetRequiredService<RoleConfigurationProvider>(),
                sp.GetService<ILogger<DataverseAccessPolicy>>()));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataverseConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger, or null for a functional provider without logging.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema the dataverse tables live in.</param>
    public DataverseConfigurationProvider(
        ILogger<DataverseConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "dataverse")
        : base(logger ?? NullLogger<DataverseConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName, "Dataverse")
    {
        _logger = logger ?? NullLogger<DataverseConfigurationProvider>.Instance;
    }

    private readonly ILogger _logger;

    /// <inheritdoc />
    public async Task<IGenericResult<DataverseMemberConfiguration>> SetMemberRole(
        Guid dataverseId, Guid memberId, string memberRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memberRole)
            || ReferenceEquals(DataverseMemberRoles.ByName(memberRole), DataverseMemberRoles.NotFound))
        {
            return GenericResult<DataverseMemberConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "field", "MemberRole", "value", memberRole ?? string.Empty));
        }

        var member = await FindChild(dataverseId, u => u.Members, m => m.Id, memberId, "member", cancellationToken).ConfigureAwait(false);
        if (member.IsFailure) return member;

        member.Value!.MemberRole = memberRole;
        var saved = await SaveChild(member.Value, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<DataverseMemberConfiguration>() : member;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<DataverseMemberConfiguration>> AddMember(
        Guid dataverseId, DataverseMemberConfiguration member, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (string.IsNullOrWhiteSpace(member.SubjectType)
            || ReferenceEquals(DataverseSubjectTypes.ByName(member.SubjectType), DataverseSubjectTypes.NotFound))
        {
            return GenericResult<DataverseMemberConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "field", "SubjectType",
                                     "value", member.SubjectType ?? string.Empty));
        }

        if (string.IsNullOrWhiteSpace(member.MemberRole)
            || ReferenceEquals(DataverseMemberRoles.ByName(member.MemberRole), DataverseMemberRoles.NotFound))
        {
            return GenericResult<DataverseMemberConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "field", "MemberRole",
                                     "value", member.MemberRole ?? string.Empty));
        }

        // State was the one column of the three with a CHECK constraint in the database and no
        // check here, so a typo was refused as a constraint violation rather than by name -- and
        // an unknown state now decides authorization (DataverseAccessPolicy reads GrantsMembership
        // off the option), which makes accepting one a way to store a membership nobody can
        // interpret.
        if (string.IsNullOrWhiteSpace(member.State)
            || ReferenceEquals(DataverseMemberStates.ByName(member.State), DataverseMemberStates.NotFound))
        {
            return GenericResult<DataverseMemberConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "field", "State",
                                     "value", member.State ?? string.Empty));
        }

        member.DataverseId = dataverseId;
        if (member.Id == Guid.Empty) member.Id = Guid.CreateVersion7();

        var saved = await SaveChild(member, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<DataverseMemberConfiguration>()
            : GenericResult<DataverseMemberConfiguration>.Success(member);
    }

    /// <inheritdoc />
    public Task<IGenericResult> RemoveMember(Guid memberId, CancellationToken cancellationToken = default)
        => DeleteChild<DataverseMemberConfiguration>(memberId, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<DataverseResourceConfiguration>> AttachResource(
        Guid dataverseId, DataverseResourceConfiguration resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        // The kind must be one this host understands. An unreferenced domain's resource cannot be
        // attached, which is the point of the collection gathering options from owning packages.
        if (string.IsNullOrWhiteSpace(resource.ResourceType)
            || ReferenceEquals(DataverseResourceKinds.ByName(resource.ResourceType), DataverseResourceKinds.NotFound))
        {
            return GenericResult<DataverseResourceConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "field", "ResourceType",
                                     "value", resource.ResourceType ?? string.Empty));
        }

        // Ownership is a property of the KIND, not a free choice at attach time: a dataverse owns the
        // data sets it sketched but only uses the shared connection it reads through.
        if (string.Equals(resource.Relationship, "Owns", StringComparison.OrdinalIgnoreCase)
            && !DataverseResourceKinds.ByName(resource.ResourceType).CanBeOwned)
        {
            return GenericResult<DataverseResourceConfiguration>.Failure(
                DataversesResultCodes.ByName("DataverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "field", "Relationship", "value", "Owns"));
        }

        resource.DataverseId = dataverseId;
        if (resource.Id == Guid.Empty) resource.Id = Guid.CreateVersion7();

        var saved = await SaveChild(resource, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<DataverseResourceConfiguration>()
            : GenericResult<DataverseResourceConfiguration>.Success(resource);
    }

    /// <inheritdoc />
    public Task<IGenericResult> DetachResource(Guid resourceId, CancellationToken cancellationToken = default)
        => DeleteChild<DataverseResourceConfiguration>(resourceId, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<DataverseRelationshipConfiguration>> SetRelationshipJoinKey(
        Guid dataverseId, Guid relationshipId, Guid? leftFieldId, Guid? rightFieldId,
        CancellationToken cancellationToken = default)
    {
        var rel = await FindChild(dataverseId, u => u.Relationships, r => r.Id, relationshipId, "relationship", cancellationToken).ConfigureAwait(false);
        if (rel.IsFailure) return rel;

        // Both sides are nullable because a relationship is drawn on the map before anyone has said
        // which columns carry it. Clearing one is a legitimate write, not an incomplete one.
        rel.Value!.LeftFieldId = leftFieldId;
        rel.Value.RightFieldId = rightFieldId;

        var saved = await SaveChild(rel.Value, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<DataverseRelationshipConfiguration>() : rel;
    }

    /// <summary>Loads the aggregate and picks one child out of it.</summary>
    /// <remarks>
    /// Reading the aggregate to locate a child is fine — it is the WRITE that must stay narrow.
    /// The id selector is passed rather than reflected: this layer stays reflection-free, the same
    /// reason the cascade sets child FKs through a generated mapper instead of a property lookup.
    /// </remarks>
    private async Task<IGenericResult<TChild>> FindChild<TChild>(
        Guid dataverseId,
        Func<DataverseConfiguration, System.Collections.Generic.IList<TChild>> select,
        Func<TChild, Guid> id,
        Guid childId,
        string childKind,
        CancellationToken cancellationToken)
        where TChild : class
    {
        var dataverse = await Get(dataverseId, cancellationToken).ConfigureAwait(false);
        if (dataverse.IsFailure) return dataverse.ToNewResult<TChild>();
        if (dataverse.Value is null)
        {
            return GenericResult<TChild>.Failure(
                DataversesResultCodes.ByName("DataverseLoadReturnedNoValue"), _logger,
                ResultDetails.Create("name", dataverseId.ToString()));
        }

        var match = select(dataverse.Value).FirstOrDefault(c => id(c) == childId);

        return match is null
            ? GenericResult<TChild>.Failure(
                DataversesResultCodes.ByName("DataverseChildNotFound"), _logger,
                ResultDetails.Create("name", dataverseId.ToString(), "kind", childKind, "id", childId.ToString()))
            : GenericResult<TChild>.Success(match);
    }
}
