using Fdw.Configuration;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Validation.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests;

/// <summary>
/// Every configuration class the host can write, and every child record its save cascade writes, has an option in
/// <see cref="ConfigurationValidators"/>: a validator, or a statement of why it needs none (FDW-3541). A class with neither is refused when it is
/// written; this test says so before a deploy does, and names every such class.
/// </summary>
public sealed class ConfigurationValidatorCoverageTests
{
    private static IEnumerable<Type> WrittenTypes()
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(
            PocoMapperCollection.All()
                .Select(mapper => mapper.TargetType)
                .Where(type => typeof(IImplementationConfiguration).IsAssignableFrom(type)));

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type))
                continue;

            yield return type;

            foreach (var child in ChildrenOf(type))
                queue.Enqueue(child);
        }
    }

    private static IEnumerable<Type> ChildrenOf(Type type)
    {
        var mapper = PocoMapperCollection.ByName(type.Name);
        return ReferenceEquals(mapper, PocoMapperCollection.NotFound)
            ? []
            : mapper.CascadeChildren.Where(child => !child.IsPropertyCollection).Select(child => child.ChildType);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void EveryWrittenConfigurationClassStatesAValidatorOrWhyItNeedsNone()
    {
        var undeclared = WrittenTypes()
            .Where(type => ConfigurationValidatorResolver.Find(type, NullLogger.Instance).IsFailure)
            .Select(type => type.FullName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        undeclared.ShouldBeEmpty("These configuration classes are written but declare neither a validator nor NoValidation (ConfigurationValidators)");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheHostWritesSomeConfigurationClassesSoTheCoverageTestIsNotEmpty()
    {
        WrittenTypes().Count().ShouldBeGreaterThan(20);
    }
}
