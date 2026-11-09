using Shouldly;
using Xunit;

namespace Fdw.Data.Abstractions.Tests.ChildCascade;

/// <summary>
/// The two modes in which a save cascades to a child collection that hangs from the owner's durable Id (FDW-4240). The generator emits the mode's
/// name, and the provider finds it by name, so the names are pinned along with what each mode does.
/// </summary>
public sealed class ChildCascadeModesTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AuthoritativeRetiresTheChildrenTheSavedListNoLongerCarries()
    {
        var mode = ChildCascadeModes.ByName("Authoritative");

        ReferenceEquals(mode, ChildCascadeModes.NotFound).ShouldBeFalse();
        mode.RetiresAbsentChildren.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void UpsertOnlyRetiresNone()
    {
        var mode = ChildCascadeModes.ByName("UpsertOnly");

        ReferenceEquals(mode, ChildCascadeModes.NotFound).ShouldBeFalse();
        mode.RetiresAbsentChildren.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ANameThatIsNotAModeIsNotFoundAndHasNoDefault()
    {
        ReferenceEquals(ChildCascadeModes.ByName("Fdw.Data.NoSuchChildCascade"), ChildCascadeModes.NotFound).ShouldBeTrue();
    }
}
