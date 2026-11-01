using System.Diagnostics.CodeAnalysis;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-915: a REST- or JSON:API-protocol connection used to render EVERY filter operator (Contains,
/// StartsWith, EndsWith, NotEqual, GreaterThan, ...) as a bare "field=value" query parameter,
/// discarding the operator entirely — so a Contains filter reached the target API with exact-match
/// semantics, silently, with no failure anywhere. These tests pin the fix: the base "Simple:
/// name=value" convention now only claims Equal (and an AND-only group of such conditions), and
/// <see cref="RestProtocol.Translate"/> refuses a request instead of mis-rendering an operator or
/// group shape it cannot express. OData is exercised separately because it genuinely supports every
/// single operator via <see cref="IFilterOperator.ODataOperator"/>, though not yet a multi-condition
/// group.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FilterOperatorTranslationTests
{
    private static readonly HttpProtocolContext Context = new(
        new Mock<IGenericConfiguration>().Object,
        new Mock<ILoggerFactory>().Object,
        null,
        null,
        null);

    private static Mock<IQueryCommand> QueryCommand(IFilterNode? filterRoot)
    {
        var command = new Mock<IQueryCommand>();
        command.Setup(c => c.CommandType).Returns("Query");
        command.Setup(c => c.Filter).Returns(filterRoot is null ? null : new FilterExpression { Root = filterRoot });
        command.Setup(c => c.Ordering).Returns((IOrderingExpression?)null);
        command.Setup(c => c.Paging).Returns((IPagingExpression?)null);
        return command;
    }

    private static Mock<IStorageContainer> Container()
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath)null!);
        container.Setup(c => c.Name).Returns("teams");
        return container;
    }

    /// <summary>The decoded request URI, so assertions read the semantic query rather than percent-escapes.</summary>
    private static string DecodedUri(HttpRequestMessage request)
        => System.Uri.UnescapeDataString(request.RequestUri!.ToString());

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Contains")]
    [InlineData("StartsWith")]
    [InlineData("EndsWith")]
    [InlineData("NotEqual")]
    [InlineData("GreaterThan")]
    public async Task RestProtocolTranslateRefusesUnsupportedOperatorInsteadOfMisrenderingIt(string operatorName)
    {
        // Arrange
        var protocol = new RestProtocol();
        var op = FilterOperators.ByName(operatorName);
        var filter = new FilterCondition { PropertyName = "Name", Operator = op, Value = "Patriots" };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert — refused specifically as UnsupportedFilterOperator, never sent as "Name=Patriots".
        // A bare IsSuccess check isn't enough: the catch-all in Translate turns ANY exception into a
        // failure too, so that alone would also pass if this were crashing instead of refusing.
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => IsUnsupportedFilterOperator(m));
    }

    private static bool IsUnsupportedFilterOperator(IGenericMessage message)
        => string.Equals(message.Code, HttpResultCodes.ByName("UnsupportedFilterOperator").Code, System.StringComparison.Ordinal);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RestProtocolTranslateRefusesOrGroupInsteadOfSilentlySendingAnd()
    {
        // Arrange — Or[Name=Patriots, Name=Jets] joined with "&" would ask the server for BOTH
        // conditions (AND, or first-value-wins on many servers), not either one.
        var protocol = new RestProtocol();
        var filter = new FilterGroup
        {
            Operator = LogicalOperator.Or,
            Nodes =
            [
                new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Jets" },
            ],
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => IsUnsupportedFilterOperator(m));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task RestProtocolTranslateBuildsAndGroupOfEqualityAsJoinedParameters()
    {
        // Arrange — an AND-only group of Equal conditions is exactly the shape a REST querystring
        // can express: each parameter narrows the result.
        var protocol = new RestProtocol();
        var filter = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                new FilterCondition { PropertyName = "Conference", Operator = FilterOperators.Equal, Value = "AFC" },
            ],
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        DecodedUri(result.Value!).ShouldContain("Name=Patriots");
        DecodedUri(result.Value!).ShouldContain("Conference=AFC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task RestProtocolTranslateBuildsEqualityAsFieldEqualsValue()
    {
        // Arrange
        var protocol = new RestProtocol();
        var filter = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.RequestUri!.ToString().ShouldContain("Name=Patriots");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Equal", true)]
    [InlineData("Contains", false)]
    [InlineData("StartsWith", false)]
    [InlineData("EndsWith", false)]
    [InlineData("NotEqual", false)]
    public void RestProtocolCanExpressFilterMatchesWhatTranslateActuallyDoes(string operatorName, bool expected)
    {
        // Arrange
        var protocol = new RestProtocol();
        var op = FilterOperators.ByName(operatorName);
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Name", Operator = op, Value = "Patriots" },
        };

        // Act
        var canExpress = protocol.CanExpressFilter(filter);

        // Assert — this must never claim true for an operator Translate would refuse (or vice versa)
        canExpress.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void RestProtocolCanExpressFilterIsFalseForOrGroup()
    {
        // Arrange
        var protocol = new RestProtocol();
        var filter = new FilterExpression
        {
            Root = new FilterGroup
            {
                Operator = LogicalOperator.Or,
                Nodes =
                [
                    new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                    new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Jets" },
                ],
            },
        };

        // Act & Assert
        protocol.CanExpressFilter(filter).ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Equal", "Name eq 'Patriots'")]
    [InlineData("NotEqual", "Name ne 'Patriots'")]
    [InlineData("GreaterThan", "Name gt 'Patriots'")]
    public async Task ODataProtocolRendersInfixOperatorsByTheirOwnODataOperator(string operatorName, string expectedFragment)
    {
        // Arrange
        var protocol = new ODataProtocol();
        var op = FilterOperators.ByName(operatorName);
        var filter = new FilterCondition { PropertyName = "Name", Operator = op, Value = "Patriots" };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert — OData genuinely supports every operator, so this must succeed, not refuse
        result.IsSuccess.ShouldBeTrue();
        DecodedUri(result.Value!).ShouldContain(expectedFragment);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Contains", "contains(Name,'Pat')")]
    [InlineData("StartsWith", "startswith(Name,'Pat')")]
    [InlineData("EndsWith", "endswith(Name,'ots')")]
    public async Task ODataProtocolRendersFunctionStyleOperatorsAsCallsNotInfix(string operatorName, string expectedFragment)
    {
        // Arrange — OData v4 has no infix form for these; "Name contains 'x'" (the shape this file
        // used to render unconditionally, and what a hand-rolled per-operator switch would produce
        // without special-casing) is invalid syntax a v4 server rejects.
        var protocol = new ODataProtocol();
        var op = FilterOperators.ByName(operatorName);
        var value = string.Equals(operatorName, "EndsWith", System.StringComparison.Ordinal) ? "ots" : "Pat";
        var filter = new FilterCondition { PropertyName = "Name", Operator = op, Value = value };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        DecodedUri(result.Value!).ShouldContain(expectedFragment);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ODataProtocolRendersGuidValuesBareNotWithTheV3LiteralWrapper()
    {
        // Arrange — IFilterOperator.FormatODataValue wraps a Guid as "guid'...'" (OData v3); this
        // codebase's OData targets v4, where a Guid is written bare. Delegating value-formatting to
        // the operator (rather than a locally-owned v4 formatter) would silently break every existing
        // Guid filter against a v4 server.
        var protocol = new ODataProtocol();
        var id = System.Guid.Parse("11111111-2222-3333-4444-555555555555");
        var filter = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.Equal, Value = id };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decoded = DecodedUri(result.Value!);
        decoded.ShouldContain($"Id eq {id}");
        decoded.ShouldNotContain("guid'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ODataProtocolEscapesASingleQuoteInsideAStringValue()
    {
        // Arrange — the code this replaced quoted a string value without escaping embedded quotes,
        // which truncates the filter's grammar at the first "'" rather than filtering on the real value.
        var protocol = new ODataProtocol();
        var filter = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "O'Brien" };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        DecodedUri(result.Value!).ShouldContain("Name eq 'O''Brien'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ODataProtocolRendersIsNullWithoutFallingBackToBareEquality()
    {
        // Arrange — the old hand-rolled switch had no "IsNull" case and fell back to "eq"; it happened
        // to render "$filter=Name eq null" anyway (its separate value formatter mapped a null Value to
        // the text "null"), so this case alone doesn't distinguish old from new -- see the IsNotNull
        // test below, where the same old fallback produced the WRONG, inverted result.
        var protocol = new ODataProtocol();
        var filter = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("IsNull"), Value = null };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        DecodedUri(result.Value!).ShouldContain("Name eq null");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ODataProtocolRendersIsNotNullWithTheOppositeOperatorFromIsNull()
    {
        // Arrange — the old code had no "IsNotNull" case either, so it ALSO fell back to "eq", meaning
        // IsNull and IsNotNull produced the identical, wrong-for-one-of-them "$filter=Name eq null".
        // This is the case that actually distinguishes the fix from the old behavior (a bare IsNull
        // check alone would pass even unfixed, by coincidence — see IsNullOperator's own null-shaped
        // value formatting).
        var protocol = new ODataProtocol();
        var filter = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("IsNotNull"), Value = null };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decoded = DecodedUri(result.Value!);
        decoded.ShouldContain("Name ne null");
        decoded.ShouldNotContain("Name eq null");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ODataProtocolCanExpressFilterIsTrueForEveryOperator()
    {
        // Arrange
        var protocol = new ODataProtocol();
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Name", Operator = FilterOperators.ByName("Contains"), Value = "Pat" },
        };

        // Act & Assert
        protocol.CanExpressFilter(filter).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ODataProtocolTranslateRefusesAMultiConditionGroupInsteadOfSendingBrokenSyntax()
    {
        // Arrange — OData's single "$filter=" parameter can hold one condition; this protocol does
        // not yet render "$filter=A eq 1 and B eq 2" for a group, so without the SupportsGroup
        // override the base's per-child "&"-joined fallback ("$filter=...&$filter=...") would go out
        // under a CanExpressFilter that claimed success.
        var protocol = new ODataProtocol();
        var filter = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                new FilterCondition { PropertyName = "Conference", Operator = FilterOperators.Equal, Value = "AFC" },
            ],
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => IsUnsupportedFilterOperator(m));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task JsonApiProtocolTranslateRefusesANestedGroupInsteadOfSilentlyDroppingIt()
    {
        // Arrange — the group loop in JsonApiProtocol.BuildFilterFromExpression only renders direct
        // IFilterCondition children; And[And[Name=Patriots, Conference=AFC], Active=true] would have
        // gone out carrying only "filter[Active]=true", silently dropping the nested group.
        var protocol = new JsonApiProtocol();
        var filter = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                        new FilterCondition { PropertyName = "Conference", Operator = FilterOperators.Equal, Value = "AFC" },
                    ],
                },
                new FilterCondition { PropertyName = "Active", Operator = FilterOperators.Equal, Value = true },
            ],
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => IsUnsupportedFilterOperator(m));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task JsonApiProtocolTranslateBuildsAFlatGroupOfConditionsCorrectly()
    {
        // Arrange — a flat (non-nested) AND group is exactly the shape JsonApiProtocol's group loop
        // handles today; SupportsNestedGroups only refuses a NESTED group, not this one.
        var protocol = new JsonApiProtocol();
        var filter = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                new FilterCondition { PropertyName = "Conference", Operator = FilterOperators.Equal, Value = "AFC" },
            ],
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decoded = DecodedUri(result.Value!);
        decoded.ShouldContain("filter[Name]=Patriots");
        decoded.ShouldContain("filter[Conference]=AFC");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task ODataProtocolRendersAnInFilterAsAJoinedListNotAToStringOfTheCollection()
    {
        // Arrange — switching OData's value formatting off IFilterOperator.FormatODataValue (done to
        // fix the Guid v3-wrapper regression) must not regress In's own array-join support: a bare
        // ToString() on a List<string> renders as the type name, not the values.
        var protocol = new ODataProtocol();
        var filter = new FilterCondition
        {
            PropertyName = "Conference",
            Operator = FilterOperators.ByName("In"),
            Value = new List<string> { "AFC", "NFC" },
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var decoded = DecodedUri(result.Value!);
        decoded.ShouldContain("Conference in ('AFC','NFC')");
        decoded.ShouldNotContain("System.Collections");
    }
}
