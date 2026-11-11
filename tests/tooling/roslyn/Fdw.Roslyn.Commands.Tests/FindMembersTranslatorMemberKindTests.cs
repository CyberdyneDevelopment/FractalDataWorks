using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Navigation.Commands;
using Fdw.Roslyn.Commands.Navigation.Results;
using Fdw.Roslyn.Commands.Navigation.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// FindMembers reads the member kinds the request names (FDW-5194): a name that is not a member kind is refused with its own code,
/// and a Roslyn symbol kind that no member of a type has (Local) with another, never skipped. The kinds are checked before the
/// document is looked up, so an empty solution is enough.
/// </summary>
public sealed class FindMembersTranslatorMemberKindTests
{
    private static Task<IGenericResult<QueryResult<IReadOnlyList<NavigationMemberInfo>>>> Find(string memberKinds) =>
        new FindMembersTranslator().Translate(
            new FindMembersCommand { FilePath = "/repo/src/Foo.cs", Line = 1, Column = 1, MemberKinds = memberKinds },
            new AdhocWorkspace().CurrentSolution,
            TestContext.Current.CancellationToken);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ANameThatIsNoMemberKindIsRefused()
    {
        var result = await Find("Method, Bogus");

        result.IsFailure.ShouldBeTrue();
        var message = result.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("'Bogus' is not a member kind");
        message.ShouldContain("NamedType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ASymbolKindNoMemberHasIsRefusedWithItsOwnCode()
    {
        var result = await Find("Local");

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("'Local' is a symbol kind that no member of a type has");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task MemberKindsAreMatchedIgnoringCaseAndTheRequestGoesOnToTheDocument()
    {
        var result = await Find("method, PROPERTY,");

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Document not found");
    }
}
