using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Universes.Abstractions;
using Fdw.Services.Universes.Commands;
using Fdw.Services.Universes.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Universes;

/// <summary>
/// Reads and writes <see cref="UniverseConfiguration"/> and its children.
/// </summary>
/// <remarks>
/// There are no Get overrides here. The base composes the aggregate — members, resources and
/// relationships come back populated — because those are direct children of the universe row.
/// <c>DataSetConfigurationProvider</c> overrides Get only to reach a grandchild, which a universe
/// does not have.
/// </remarks>
public class UniverseConfigurationProvider
    : ImplementationConfigurationProviderBase<UniverseConfiguration, UniverseConfigurationCommand>,
      IUniverseConfigurationProvider
{
    /// <summary>
    /// Registers the provider and the interfaces callers resolve it through.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<UniverseConfigurationProvider>(sp =>
            new UniverseConfigurationProvider(
                sp.GetService<ILogger<UniverseConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStoreTypes.ConfigurationConnection,
                "universe"));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<UniverseConfiguration, UniverseConfigurationCommand>>(
            sp => sp.GetRequiredService<UniverseConfigurationProvider>());

        services.TryAddSingleton<IUniverseConfigurationProvider>(
            sp => sp.GetRequiredService<UniverseConfigurationProvider>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UniverseConfigurationProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger, or null for a functional provider without logging.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema the universe tables live in.</param>
    public UniverseConfigurationProvider(
        ILogger<UniverseConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "universe")
        : base(logger ?? NullLogger<UniverseConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
        _logger = logger ?? NullLogger<UniverseConfigurationProvider>.Instance;
    }

    private readonly ILogger _logger;

    /// <inheritdoc />
    public async Task<IGenericResult<UniverseMemberConfiguration>> SetMemberRole(
        Guid universeId, Guid memberId, string memberRole, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(memberRole)
            || ReferenceEquals(UniverseMemberRoles.ByName(memberRole), UniverseMemberRoles.NotFound))
        {
            return GenericResult<UniverseMemberConfiguration>.Failure(
                UniversesResultCodes.ByName("UniverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", universeId.ToString(), "field", "MemberRole", "value", memberRole ?? string.Empty));
        }

        var member = await FindChild(universeId, u => u.Members, m => m.Id, memberId, "member", cancellationToken).ConfigureAwait(false);
        if (member.IsFailure) return member;

        member.Value!.MemberRole = memberRole;
        var saved = await SaveChild(member.Value, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<UniverseMemberConfiguration>() : member;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<UniverseMemberConfiguration>> AddMember(
        Guid universeId, UniverseMemberConfiguration member, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (string.IsNullOrWhiteSpace(member.SubjectType)
            || ReferenceEquals(UniverseSubjectTypes.ByName(member.SubjectType), UniverseSubjectTypes.NotFound))
        {
            return GenericResult<UniverseMemberConfiguration>.Failure(
                UniversesResultCodes.ByName("UniverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", universeId.ToString(), "field", "SubjectType",
                                     "value", member.SubjectType ?? string.Empty));
        }

        if (string.IsNullOrWhiteSpace(member.MemberRole)
            || ReferenceEquals(UniverseMemberRoles.ByName(member.MemberRole), UniverseMemberRoles.NotFound))
        {
            return GenericResult<UniverseMemberConfiguration>.Failure(
                UniversesResultCodes.ByName("UniverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", universeId.ToString(), "field", "MemberRole",
                                     "value", member.MemberRole ?? string.Empty));
        }

        member.UniverseId = universeId;
        if (member.Id == Guid.Empty) member.Id = Guid.CreateVersion7();

        var saved = await SaveChild(member, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<UniverseMemberConfiguration>()
            : GenericResult<UniverseMemberConfiguration>.Success(member);
    }

    /// <inheritdoc />
    public Task<IGenericResult> RemoveMember(Guid memberId, CancellationToken cancellationToken = default)
        => DeleteChild<UniverseMemberConfiguration>(memberId, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<UniverseResourceConfiguration>> AttachResource(
        Guid universeId, UniverseResourceConfiguration resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);

        // The kind must be one this host understands. An unreferenced domain's resource cannot be
        // attached, which is the point of the collection gathering options from owning packages.
        if (string.IsNullOrWhiteSpace(resource.ResourceType)
            || ReferenceEquals(UniverseResourceKinds.ByName(resource.ResourceType), UniverseResourceKinds.NotFound))
        {
            return GenericResult<UniverseResourceConfiguration>.Failure(
                UniversesResultCodes.ByName("UniverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", universeId.ToString(), "field", "ResourceType",
                                     "value", resource.ResourceType ?? string.Empty));
        }

        // Ownership is a property of the KIND, not a free choice at attach time: a universe owns the
        // data sets it sketched but only uses the shared connection it reads through.
        if (string.Equals(resource.Relationship, "Owns", StringComparison.OrdinalIgnoreCase)
            && !UniverseResourceKinds.ByName(resource.ResourceType).CanBeOwned)
        {
            return GenericResult<UniverseResourceConfiguration>.Failure(
                UniversesResultCodes.ByName("UniverseLifecycleValueInvalid"), _logger,
                ResultDetails.Create("name", universeId.ToString(), "field", "Relationship", "value", "Owns"));
        }

        resource.UniverseId = universeId;
        if (resource.Id == Guid.Empty) resource.Id = Guid.CreateVersion7();

        var saved = await SaveChild(resource, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure
            ? saved.ToNewResult<UniverseResourceConfiguration>()
            : GenericResult<UniverseResourceConfiguration>.Success(resource);
    }

    /// <inheritdoc />
    public Task<IGenericResult> DetachResource(Guid resourceId, CancellationToken cancellationToken = default)
        => DeleteChild<UniverseResourceConfiguration>(resourceId, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<UniverseRelationshipConfiguration>> SetRelationshipJoinKey(
        Guid universeId, Guid relationshipId, Guid? leftFieldId, Guid? rightFieldId,
        CancellationToken cancellationToken = default)
    {
        var rel = await FindChild(universeId, u => u.Relationships, r => r.Id, relationshipId, "relationship", cancellationToken).ConfigureAwait(false);
        if (rel.IsFailure) return rel;

        // Both sides are nullable because a relationship is drawn on the map before anyone has said
        // which columns carry it. Clearing one is a legitimate write, not an incomplete one.
        rel.Value!.LeftFieldId = leftFieldId;
        rel.Value.RightFieldId = rightFieldId;

        var saved = await SaveChild(rel.Value, cancellationToken).ConfigureAwait(false);
        return saved.IsFailure ? saved.ToNewResult<UniverseRelationshipConfiguration>() : rel;
    }

    /// <summary>Loads the aggregate and picks one child out of it.</summary>
    /// <remarks>
    /// Reading the aggregate to locate a child is fine — it is the WRITE that must stay narrow.
    /// The id selector is passed rather than reflected: this layer stays reflection-free, the same
    /// reason the cascade sets child FKs through a generated mapper instead of a property lookup.
    /// </remarks>
    private async Task<IGenericResult<TChild>> FindChild<TChild>(
        Guid universeId,
        Func<UniverseConfiguration, System.Collections.Generic.IList<TChild>> select,
        Func<TChild, Guid> id,
        Guid childId,
        string childKind,
        CancellationToken cancellationToken)
        where TChild : class
    {
        var universe = await Get(universeId, cancellationToken).ConfigureAwait(false);
        if (universe.IsFailure) return universe.ToNewResult<TChild>();
        if (universe.Value is null)
        {
            return GenericResult<TChild>.Failure(
                UniversesResultCodes.ByName("UniverseLoadReturnedNoValue"), _logger,
                ResultDetails.Create("name", universeId.ToString()));
        }

        var match = select(universe.Value).FirstOrDefault(c => id(c) == childId);

        return match is null
            ? GenericResult<TChild>.Failure(
                UniversesResultCodes.ByName("UniverseChildNotFound"), _logger,
                ResultDetails.Create("name", universeId.ToString(), "kind", childKind, "id", childId.ToString()))
            : GenericResult<TChild>.Success(match);
    }
}
