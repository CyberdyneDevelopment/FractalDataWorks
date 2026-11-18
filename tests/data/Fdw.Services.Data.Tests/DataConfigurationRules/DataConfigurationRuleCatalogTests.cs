using System;
using System.Linq;
using Fdw.Services.Data.Abstractions.DataConfigurationRules;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests.DataConfigurationRules;

/// <summary>FDW-6571: the one catalog of what can change in a rule: its members, the ones a change may clear, and the kinds of change the service records.</summary>
public sealed class DataConfigurationRuleCatalogTests
{
    [Fact]
    public void ARuleMayClearItsDescriptionAndRequiredPermissionAndAValueItsNameAbbreviationAndDescription()
    {
        DataConfigurationRuleCatalog.Clearable(DataConfigurationRuleCatalog.RuleOwner).ShouldBe(["description", "requiredPermission"], ignoreOrder: true);
        DataConfigurationRuleCatalog.Clearable(DataConfigurationRuleCatalog.ValueOwner).ShouldBe(["name", "abbreviation", "description"], ignoreOrder: true);
    }

    [Fact]
    public void EveryChangeKindThatNamesAMemberNamesOneTheCatalogHas()
    {
        foreach (var kind in DataConfigurationRuleCatalog.ChangeKinds.Where(kind => kind.Member is not null))
            DataConfigurationRuleCatalog.Member(kind.MemberOf!, kind.Member!).ShouldNotBeNull($"{kind.Name} names {kind.MemberOf}.{kind.Member}");
    }

    [Fact]
    public void NoNameRepeatsWithinAnOwnerOrAmongTheChangeKinds()
    {
        DataConfigurationRuleCatalog.Members.GroupBy(member => (member.Of, member.Name)).ShouldAllBe(group => group.Count() == 1);
        DataConfigurationRuleCatalog.ChangeKinds.Select(kind => kind.Name).Distinct(StringComparer.Ordinal).Count().ShouldBe(DataConfigurationRuleCatalog.ChangeKinds.Count);
    }

    [Fact]
    public void AKindThatIsNotInTheCatalogIsNull()
    {
        DataConfigurationRuleCatalog.ChangeKind("Mystery").ShouldBeNull();
        DataConfigurationRuleCatalog.Member("rule", "mystery").ShouldBeNull();
    }
}
