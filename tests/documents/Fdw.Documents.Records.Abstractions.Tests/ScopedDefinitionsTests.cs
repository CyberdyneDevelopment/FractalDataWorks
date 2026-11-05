using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Results;

namespace Fdw.Documents.Records.Abstractions.Tests;

/// <summary>
/// Tests for FDW's rule for document definitions (FDW-3398): the most specific level's definition is in force unless a level above locked
/// the code, an organization's own definition holds only for it, and a code no level defines is refused, never defaulted.
/// </summary>
public sealed class ScopedDefinitionsTests
{
    private static readonly Guid Rayburn = new("11111111-0000-0000-0000-000000000003");
    private static readonly Guid Acme = new("11111111-0000-0000-0000-000000000004");
    private static readonly Guid Finance = new("44444444-0000-0000-0000-000000000001");
    private static readonly Guid Legal = new("44444444-0000-0000-0000-000000000002");
    private static readonly string[] FinanceSees = ["Finance contract", "Rayburn invoice"];

    private sealed record DocumentType(string Code, string Name, Guid? TenantId = null, Guid? OrganizationId = null, bool Locked = false)
        : IDocumentTypeDefinition
    {
        public Guid Id { get; } = Guid.CreateVersion7();
    }

    private static DocumentType Platform(string name, bool locked = false) => new("Contract", name, Locked: locked);

    private static DocumentType Tenant(string name, bool locked = false, Guid? tenant = null) => new("contract", name, tenant ?? Rayburn, Locked: locked);

    private static DocumentType Organization(string name, Guid? organization = null) => new("CONTRACT", name, Rayburn, organization ?? Finance);

    private static EffectiveDefinition<DocumentType> InForce(IReadOnlyList<DocumentType> rows, Guid? organization = null)
    {
        var effective = ScopedDefinitions.Effective(rows, Rayburn, organization ?? Finance, "Contract");
        effective.IsSuccess.ShouldBeTrue(effective.CurrentMessage);
        return effective.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ThePlatformsDefinitionIsInForceWhereNoLevelBelowDefinesTheCode()
    {
        var effective = InForce([Platform("Platform contract")]);

        effective.Definition.Name.ShouldBe("Platform contract");
        effective.SetAt.ShouldBe(DefinitionLevels.Platform);
        effective.LockedAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void TheMostSpecificLevelWinsAndCodesAreComparedWithoutCase()
    {
        var tenantWins = InForce([Platform("Platform contract"), Tenant("Rayburn contract")]);
        var organizationWins = InForce([Platform("Platform contract"), Tenant("Rayburn contract"), Organization("Finance contract")]);

        tenantWins.Definition.Name.ShouldBe("Rayburn contract");
        tenantWins.SetAt.ShouldBe(DefinitionLevels.Tenant);
        organizationWins.Definition.Name.ShouldBe("Finance contract");
        organizationWins.SetAt.ShouldBe(DefinitionLevels.Organization);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void APlatformLockKeepsTheLevelsBelowFromApplyingAndFromDefiningTheCode()
    {
        IReadOnlyList<DocumentType> rows = [Platform("Platform contract", locked: true), Tenant("Rayburn contract"), Organization("Finance contract")];

        var effective = InForce(rows);
        var tenantMay = ScopedDefinitions.MayDefine(rows, Rayburn, null, "contract", locked: false);
        var organizationMay = ScopedDefinitions.MayDefine(rows, Rayburn, Finance, "contract", locked: false);

        effective.Definition.Name.ShouldBe("Platform contract");
        effective.LockedAt.ShouldBe(DefinitionLevels.Platform);
        tenantMay.IsSuccess.ShouldBeFalse();
        tenantMay.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionLocked"));
        organizationMay.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ATenantLockGovernsItsOrganizationsButNotAnotherTenants()
    {
        IReadOnlyList<DocumentType> rows = [Tenant("Rayburn contract", locked: true), Organization("Finance contract"), Tenant("Acme contract", tenant: Acme)];

        var effective = InForce(rows);
        var organizationMay = ScopedDefinitions.MayDefine(rows, Rayburn, Finance, "Contract", locked: false);
        var otherTenantMay = ScopedDefinitions.MayDefine(rows, Acme, Legal, "Contract", locked: false);

        effective.Definition.Name.ShouldBe("Rayburn contract");
        effective.LockedAt.ShouldBe(DefinitionLevels.Tenant);
        organizationMay.IsSuccess.ShouldBeFalse();
        otherTenantMay.IsSuccess.ShouldBeTrue(otherTenantMay.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AnOrganizationCannotLockAndAnOrganizationRowMarkedLockedLocksNothing()
    {
        IReadOnlyList<DocumentType> rows = [new("Contract", "Finance contract", Rayburn, Finance, Locked: true)];

        var may = ScopedDefinitions.MayDefine(rows, Rayburn, Finance, "Contract", locked: true);
        var effective = InForce(rows);

        may.IsSuccess.ShouldBeFalse();
        may.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionLocked"));
        effective.LockedAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void AnOrganizationsOwnDefinitionHoldsForItAndNeverForAnotherOrganization()
    {
        IReadOnlyList<DocumentType> rows = [Organization("Finance contract")];

        var finance = ScopedDefinitions.Effective(rows, Rayburn, Finance, "Contract");
        var legal = ScopedDefinitions.Effective(rows, Rayburn, Legal, "Contract");
        var otherTenant = ScopedDefinitions.Effective(rows, Acme, Finance, "Contract");

        finance.IsSuccess.ShouldBeTrue(finance.CurrentMessage);
        legal.IsSuccess.ShouldBeFalse();
        legal.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionNotConfigured"));
        otherTenant.IsSuccess.ShouldBeTrue(otherTenant.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ACodeNoLevelDefinesIsRefusedNamingItNeverDefaulted()
    {
        var result = ScopedDefinitions.Effective([Platform("Platform contract")], Rayburn, Finance, "Invoice");

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionNotConfigured"));
        var shown = result.Code.ShouldNotBeNull().FormatMessage(result.Details);
        shown.ShouldContain("Invoice");
        shown.ShouldContain(Finance.ToString("D", System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TwoDefinitionsOfACodeAtOneLevelInForceAreRefusedAsAmbiguous()
    {
        var result = ScopedDefinitions.Effective([Tenant("One"), Tenant("Two")], Rayburn, Finance, "Contract");

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldBeSameAs(DocumentRecordResultCodes.ByName("DocumentDefinitionAmbiguous"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void AllResolvesEveryCodeOnceAndLeavesOutOtherOrganizations()
    {
        IReadOnlyList<DocumentType> rows =
        [
            Platform("Platform contract"),
            Organization("Finance contract"),
            new("Invoice", "Rayburn invoice", Rayburn),
            new("Memo", "Legal memo", Rayburn, Legal),
        ];

        var all = ScopedDefinitions.All(rows, Rayburn, Finance);

        all.IsSuccess.ShouldBeTrue(all.CurrentMessage);
        all.Value.ShouldNotBeNull().Select(effective => effective.Definition.Name).ShouldBe(FinanceSees, ignoreOrder: true);
    }
}
