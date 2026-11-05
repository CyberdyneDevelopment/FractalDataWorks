using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Xunit;

namespace Fdw.Collections.SourceGenerators.Tests;

/// <summary>
/// TC009, the NotFound sentinel guard, run against each real collection generator (FDW-3467).
/// </summary>
/// <remarks>
/// Every collection kind emits a NotFound sentinel that derives from the option base and calls its protected
/// constructor in the collection's static initializer. A parameter the sentinel cannot build is passed null. When
/// the base rejects null, the first touch of the collection throws; in FDW-2931 that stopped every host at startup.
/// Each kind must report the hostile parameter, and must build a NotFound for a base it can satisfy.
/// </remarks>
public class NotFoundSentinelGuardTests
{
    private const string TypeOptionUsings = """
        using System;
        using System.Collections.Generic;
        using Fdw.Collections;
        using Fdw.Collections.Attributes;

        namespace Test;

        """;

    private const string ServiceTypeUsings = """
        using System;
        using System.Collections.Generic;
        using Fdw.Abstractions;
        using Fdw.Collections;
        using Fdw.Configuration;
        using Fdw.ServiceTypes;

        namespace Test;

        public interface IWidgetService : IGenericService { }
        public interface IWidgetConfiguration : IGenericConfiguration { }
        public interface IWidgetFactory : IServiceFactory<IWidgetService, IWidgetConfiguration> { }
        public interface IWidgetType : IServiceType { }

        """;

    private const string HostileParameter = "Func<string> factory";

    private const string GoodParameters = "int order, IReadOnlyList<string> tags, string[] aliases, DayOfWeek day, Uri? homepage";

    private static readonly Dictionary<string, SentinelGuardCase> Cases = new[]
    {
        TypeOptionCase("TypeCollection", "TypeCollection", () => new TypeCollectionGenerator()),
        TypeOptionCase("MutableTypeCollection", "MutableTypeCollection", () => new MutableTypeCollectionGenerator()),
        TypeOptionCase("TypeInstanceCollection", "TypeInstanceCollection", () => new TypeInstanceCollectionGenerator()),
        ServiceTypeCase("ServiceTypeCollection", "ServiceTypeCollectionBase", () => new ServiceTypeCollectionGenerator()),
        ServiceTypeCase("MutableServiceTypeCollection", "MutableServiceTypeCollectionBase", () => new MutableServiceTypeCollectionGenerator()),
        ServiceTypeCase("ServiceTypeInstanceCollection", "ServiceTypeCollectionBase", () => new ServiceTypeInstanceCollectionGenerator()),
    }.ToDictionary(c => c.Name, System.StringComparer.Ordinal);

    /// <summary>The six collection kinds, by name.</summary>
    public static TheoryData<string> Kinds => new(Cases.Keys);

    [Theory]
    [MemberData(nameof(Kinds))]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void HostileConstructorParameterReportsTc009(string kind)
    {
        var testCase = Cases[kind];

        var (_, diagnostics) = RealGeneratorHelper.Run(testCase.CreateGenerator(), testCase.BuildSource(HostileParameter));

        var tc009 = diagnostics.Where(d => string.Equals(d.Id, "TC009", System.StringComparison.Ordinal)).ToList();
        tc009.Count.ShouldBe(1, $"{kind} must report the Func<string> parameter once");
        var message = tc009[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture);
        message.ShouldContain(testCase.CollectionTypeName.Split('.').Last());
        message.ShouldContain("factory");
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void BuildableConstructorParametersBuildNotFoundAtRuntime(string kind)
    {
        var testCase = Cases[kind];

        var (output, diagnostics) = RealGeneratorHelper.Run(testCase.CreateGenerator(), testCase.BuildSource(GoodParameters));

        diagnostics.Where(d => string.Equals(d.Id, "TC009", System.StringComparison.Ordinal)).ShouldBeEmpty();
        RealGeneratorHelper.Errors(output).ShouldBeEmpty();
        RealGeneratorHelper.ReadNotFound(output, testCase.CollectionTypeName).ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "SourceGen")]
    public void HostileConstructorParameterReportsTc009WhenTheCollectionHasOptions()
    {
        var source = Cases["TypeCollection"].BuildSource(HostileParameter) + """

            [TypeOption(typeof(Widgets), "Gear")]
            public sealed class Gear : WidgetBase
            {
                public Gear() : base(1, "Gear", () => "gear") { }
            }
            """;

        var (_, diagnostics) = RealGeneratorHelper.Run(new TypeCollectionGenerator(), source);

        diagnostics.Count(d => string.Equals(d.Id, "TC009", System.StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "SourceGen")]
    public void OptionNamedNotFoundReplacesTheSentinelSoNothingIsReported()
    {
        var source = Cases["TypeCollection"].BuildSource(HostileParameter) + """

            [TypeOption(typeof(Widgets), "NotFound")]
            public sealed class NotFoundWidget : WidgetBase
            {
                public NotFoundWidget() : base(0, "NotFound", () => "none") { }
            }
            """;

        var (_, diagnostics) = RealGeneratorHelper.Run(new TypeCollectionGenerator(), source);

        diagnostics.Where(d => string.Equals(d.Id, "TC009", System.StringComparison.Ordinal)).ShouldBeEmpty();
    }

    private static SentinelGuardCase TypeOptionCase(
        string name,
        string attribute,
        System.Func<Microsoft.CodeAnalysis.IIncrementalGenerator> createGenerator)
    {
        return new SentinelGuardCase(
            name,
            "Test.Widgets",
            createGenerator,
            parameters => TypeOptionUsings + $$"""
                public abstract class WidgetBase : TypeOptionBase<int, WidgetBase>
                {
                    protected WidgetBase(int id, string name, {{parameters}}) : base(id, name) { }
                }

                [{{attribute}}(typeof(WidgetBase), typeof(WidgetBase), typeof(Widgets))]
                public partial class Widgets : TypeCollectionBase<WidgetBase, WidgetBase>
                {
                }
                """);
    }

    private static SentinelGuardCase ServiceTypeCase(
        string name,
        string collectionBase,
        System.Func<Microsoft.CodeAnalysis.IIncrementalGenerator> createGenerator)
    {
        return new SentinelGuardCase(
            name,
            "Test.WidgetTypes",
            createGenerator,
            parameters => ServiceTypeUsings + $$"""
                public abstract class WidgetTypeBase
                    : ServiceTypeBase<IWidgetService, IWidgetFactory, IWidgetConfiguration>, IWidgetType
                {
                    protected WidgetTypeBase(string name, {{parameters}}) : base(name, "Widgets", name, name) { }
                }

                [{{name}}(typeof(WidgetTypeBase), typeof(IWidgetType), typeof(WidgetTypes))]
                public partial class WidgetTypes : {{collectionBase}}<WidgetTypeBase, IWidgetType>
                {
                }
                """);
    }
}
