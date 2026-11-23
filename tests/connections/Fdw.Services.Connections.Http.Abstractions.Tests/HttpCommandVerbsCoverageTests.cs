using System.Reflection;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

/// <summary>
/// A data command type that reaches an HTTP protocol needs an option in <see cref="HttpCommandVerbs"/>, or the request is refused (FDW-4403).
/// The command types are read from the <c>[TypeOption]</c> declarations of the loaded Fdw assemblies, not from the collections' runtime
/// registration, which holds only what the test project's registration generator saw and was empty in a test process (r159). The options are
/// declared in <c>Fdw.Commands.Data</c>, which the walk loads explicitly (a referenced assembly nothing uses is never loaded: r168 found 0). A guard
/// asserts the walk found every FDW type, so an empty walk can never pass.
/// </summary>
public sealed class HttpCommandVerbsCoverageTests
{
    // The 16 data command types FDW declares: DataCommands 12, ScriptedDataCommands 3, VectorizedDataCommands 1.
    private const int FdwDataCommandTypes = 16;

    private static readonly Type[] CommandCollections = [typeof(DataCommands), typeof(ScriptedDataCommands), typeof(VectorizedDataCommands)];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void EveryDataCommandTypeHasAnHttpVerb()
    {
        var typeNames = DeclaredDataCommandTypes();

        typeNames.Count.ShouldBeGreaterThanOrEqualTo(
            FdwDataCommandTypes,
            $"the walk found only {typeNames.Count} data command types ({string.Join(", ", typeNames)}); an empty or short walk would assert nothing");

        var withoutVerb = typeNames
            .Where(name => ReferenceEquals(HttpCommandVerbs.ByName(name), HttpCommandVerbs.NotFound))
            .ToList();

        withoutVerb.ShouldBeEmpty(
            $"these data command types have no HttpCommandVerbs option: {string.Join(", ", withoutVerb)}; add one with the verb the type had (POST and a GraphQL query unless it names another), never a catch-all");
    }

    private static List<string> DeclaredDataCommandTypes() =>
        AppDomain.CurrentDomain.GetAssemblies()
            .Append(typeof(DataCommands).Assembly)
            .Append(typeof(BulkDeleteCommand<>).Assembly)
            .Distinct()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Fdw", StringComparison.Ordinal) == true)
            .SelectMany(TypesOf)
            .SelectMany(type => type.GetCustomAttributes<TypeOptionAttribute>(false))
            .Where(option => CommandCollections.Contains(option.CollectionType))
            .Select(option => option.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
