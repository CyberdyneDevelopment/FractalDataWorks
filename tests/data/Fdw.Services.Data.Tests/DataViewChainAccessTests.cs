using System;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5446: the user who owns a data view owns its chain; nobody owns a Scoped variation's chain by this answer, and a caller who is not a signed-in user owns nothing.</summary>
public sealed class DataViewChainAccessTests
{
    private static readonly Guid Owner = Guid.NewGuid();

    private static DataViewChainAccess As(string? userId)
    {
        var accessor = new AuthenticationContextAccessor { Current = null };
        if (userId is not null)
        {
            var caller = new Mock<IAuthenticationContext>();
            caller.Setup(c => c.UserId).Returns(userId);
            accessor.Current = caller.Object;
        }

        return new DataViewChainAccess(accessor);
    }

    private static VariationDataSetConfiguration DataView() => new()
    {
        Id = Guid.NewGuid(), Name = "mine", VariationName = "mine", Implementation = VariationDataSetConfiguration.ImplementationName, OwnerUserId = Owner, BaseDataSetId = Guid.NewGuid(),
    };

    private static async Task<bool> Owns(DataViewChainAccess access, VariationDataSetConfiguration view)
    {
        var answer = await access.MayShowChain(view, TestContext.Current.CancellationToken);
        answer.IsSuccess.ShouldBeTrue(answer.CurrentMessage);
        return answer.Value;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task TheUserWhoOwnsADataViewOwnsItsChain() => (await Owns(As(Owner.ToString()), DataView())).ShouldBeTrue();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AnotherUserWhoHoldsTheViewDoesNot() => (await Owns(As(Guid.NewGuid().ToString()), DataView())).ShouldBeFalse();

    [Theory]
    [InlineData(null)]
    [InlineData("system")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task ACallerWhoIsNotASignedInUserOwnsNothing(string? userId) => (await Owns(As(userId), DataView())).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AScopedVariationsChainIsNotShownByThisAnswerEvenToTheUserWhoMadeIt()
    {
        var scoped = DataView();
        scoped.OwnerWorkspaceId = Guid.NewGuid();

        (await Owns(As(Owner.ToString()), scoped)).ShouldBeFalse();
    }
}
