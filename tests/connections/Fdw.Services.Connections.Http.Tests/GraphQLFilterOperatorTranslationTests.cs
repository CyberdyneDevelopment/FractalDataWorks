using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Messages;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;
using Fdw.Services.Connections.Http.Protocols;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Tests;

/// <summary>
/// FDW-916: GraphQLProtocolBase.MapOperatorToGraphQL named 11 operators explicitly and silently
/// mapped anything else to "eq" -- IsNull and IsNotNull, neither of which was in that list, rendered
/// identically, inverted for one of them, with CanExpressFilter claiming unconditional support. Both
/// are now named explicitly (this class already has the "eq"/"ne" vocabulary IsNull/IsNotNull need),
/// so the fix is: name every operator that genuinely has a mapping, and refuse -- rather than
/// silently mis-map -- anything that doesn't. GraphQL's group rendering (unlike REST/JsonApi/OData)
/// already correctly recurses into nested groups, so only per-operator support needs gating.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GraphQLFilterOperatorTranslationTests
{
    [ExcludeFromCodeCoverage]
    private sealed class TestGraphQLProtocol : GraphQLProtocolBase
    {
        public TestGraphQLProtocol() : base("TestGraphQL", "Test GraphQL protocol")
        {
        }
    }

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
        command.Setup(c => c.Metadata).Returns(new Dictionary<string, object>());
        return command;
    }

    private static Mock<IStorageContainer> Container()
    {
        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Path).Returns((IPath)null!);
        container.Setup(c => c.Name).Returns("teams");
        // Schema is left unconfigured — BuildFieldSelection treats a null Schema/Fields as "use the
        // default field list", which is all these tests need (they exercise filter-operator gating,
        // not field selection).
        return container;
    }

    private static bool IsUnsupportedFilterOperator(IGenericMessage message)
        => string.Equals(message.Code, HttpResultCodes.ByName("UnsupportedFilterOperator").Code, System.StringComparison.Ordinal);

    /// <summary>Deserializes the GraphQL request body and returns the "variables.filter" object as JSON text.</summary>
    private static async Task<string> FilterVariableJson(HttpRequestMessage request)
    {
        var body = await request.Content!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("variables").GetProperty("filter").GetRawText();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateRefusesAnOperatorMapOperatorToGraphQLDoesNotName()
    {
        // Arrange — every operator FilterOperators actually defines today has a GraphQL mapping now
        // (including IsNull/IsNotNull, fixed by this ticket), so exercising the refusal path needs a
        // synthetic operator standing in for one a future package might register without also adding
        // it here — the same "closed extensible set" this codebase's TypeCollections are meant to
        // handle safely rather than crash on.
        var protocol = new TestGraphQLProtocol();
        var futureOperator = new Mock<IFilterOperator>();
        futureOperator.Setup(o => o.Name).Returns("SomeFutureOperator");
        var filter = new FilterCondition { PropertyName = "Name", Operator = futureOperator.Object, Value = "x" };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert — refused, never silently rendered as "eq"
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldContain(m => IsUnsupportedFilterOperator(m));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void CanExpressFilterIsFalseForAnOperatorMapOperatorToGraphQLDoesNotName()
    {
        // Arrange
        var protocol = new TestGraphQLProtocol();
        var futureOperator = new Mock<IFilterOperator>();
        futureOperator.Setup(o => o.Name).Returns("SomeFutureOperator");
        var filter = new FilterExpression
        {
            Root = new FilterCondition { PropertyName = "Name", Operator = futureOperator.Object, Value = "x" },
        };

        // Act & Assert — this must never claim true for an operator Translate would refuse
        protocol.CanExpressFilter(filter).ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Equal", "eq")]
    [InlineData("NotEqual", "ne")]
    [InlineData("GreaterThan", "gt")]
    [InlineData("GreaterThanOrEqual", "gte")]
    [InlineData("LessThan", "lt")]
    [InlineData("LessThanOrEqual", "lte")]
    [InlineData("Contains", "contains")]
    [InlineData("StartsWith", "startsWith")]
    [InlineData("EndsWith", "endsWith")]
    [InlineData("In", "in")]
    public async Task TranslateRendersEveryOperatorUnderItsOwnGraphQLKey(string operatorName, string expectedKey)
    {
        // Arrange
        var protocol = new TestGraphQLProtocol();
        var op = FilterOperators.ByName(operatorName);
        object value = string.Equals(operatorName, "In", System.StringComparison.Ordinal)
            ? new List<string> { "AFC", "NFC" }
            : "Patriots";
        var filter = new FilterCondition { PropertyName = "Name", Operator = op, Value = value };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert — not just IsSuccess: the rendered key must be the RIGHT one, so a regression that
        // silently mapped e.g. Contains back to "eq" would actually be caught.
        result.IsSuccess.ShouldBeTrue();
        var filterJson = await FilterVariableJson(result.Value!);
        filterJson.ShouldContain($"\"Name\":{{\"{expectedKey}\":");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("IsNull", "eq")]
    [InlineData("IsNotNull", "ne")]
    public async Task TranslateRendersIsNullAndIsNotNullWithOppositeKeys(string operatorName, string expectedKey)
    {
        // Arrange — the bug this ticket fixes: both used to render as "eq" (via the old silent
        // fallback), inverting the meaning of IsNotNull. This is the case that actually distinguishes
        // fixed from unfixed, the same way FDW-915's OData IsNotNull test does.
        var protocol = new TestGraphQLProtocol();
        var op = FilterOperators.ByName(operatorName);
        var filter = new FilterCondition { PropertyName = "Name", Operator = op, Value = null };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var filterJson = await FilterVariableJson(result.Value!);
        filterJson.ShouldContain($"\"Name\":{{\"{expectedKey}\":null}}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public async Task TranslateBuildsANestedGroupCorrectly()
    {
        // Arrange — unlike REST/JsonApi/OData, GraphQL's group rendering already recurses into a
        // nested FilterGroup and renders both AND/OR via GraphQL's structured JSON shape; this pins
        // that behavior so a future change to the operator gate can't silently start refusing it too.
        var protocol = new TestGraphQLProtocol();
        var filter = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterGroup
                {
                    Operator = LogicalOperator.Or,
                    Nodes =
                    [
                        new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Patriots" },
                        new FilterCondition { PropertyName = "Name", Operator = FilterOperators.Equal, Value = "Jets" },
                    ],
                },
                new FilterCondition { PropertyName = "Active", Operator = FilterOperators.ByName("IsNotNull"), Value = null },
            ],
        };
        var command = QueryCommand(filter);

        // Act
        var result = await protocol.Translate(
            command.Object, Container().Object, Context, TestContext.Current.CancellationToken);

        // Assert — the output is deterministic, so an exact match catches a regression that flattened
        // OR into a sibling of AND, dropped a child, or mapped IsNotNull back to "eq" that three loose
        // substring checks would all miss.
        result.IsSuccess.ShouldBeTrue();
        var filterJson = await FilterVariableJson(result.Value!);
        filterJson.ShouldBe(
            "{\"AND\":[{\"OR\":[{\"Name\":{\"eq\":\"Patriots\"}},{\"Name\":{\"eq\":\"Jets\"}}]},{\"Active\":{\"ne\":null}}]}");
    }
}
