using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>Reads the attribute definitions that bear on a tenant, or on an organization of it: the platform's, the tenant's and the organization's rows (FDW-3673).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DocumentRecordCommands), "ReadAttributeDefinitions", RestrictToCurrentCompilation = true)]
public sealed class ReadAttributeDefinitionsCommand : DocumentRecordCommandBase<System.Collections.Generic.IReadOnlyList<Definitions.IAttributeDefinition>>
{
    /// <summary>Initializes the unfilled instance the collection registers; a real command comes from the other constructor.</summary>
    public ReadAttributeDefinitionsCommand()
        : base("ReadAttributeDefinitions")
    {
        TenantId = Guid.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ReadAttributeDefinitionsCommand"/> class.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="organizationId">The organization, or null for the tenant's own view.</param>
    public ReadAttributeDefinitionsCommand(Guid tenantId, Guid? organizationId)
        : base("ReadAttributeDefinitions")
    {
        TenantId = tenantId;
        OrganizationId = organizationId;
    }

    /// <summary>Gets the tenant.</summary>
    public Guid TenantId { get; }

    /// <summary>Gets the organization, or null for the tenant's own view.</summary>
    public Guid? OrganizationId { get; }

    /// <inheritdoc />
    public override string? FirstProblem() =>
        First(TenantProblem(TenantId));
}
