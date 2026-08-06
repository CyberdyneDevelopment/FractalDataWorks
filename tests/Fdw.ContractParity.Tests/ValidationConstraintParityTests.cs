using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Fdw.ContractParity.Tests;

/// <summary>
/// Guards the OTHER half of contract parity: where <see cref="ContractParityTests"/> asserts that a
/// client DTO's fields EXIST on its server counterpart, this asserts that the shared fields carry the
/// SAME validation rules.
/// <para>
/// Why this exists: CreateUserRequest advertised <c>StringLength(100)</c> on the client while the
/// server enforced <c>MaxLength(50)</c>, so a 51-100 character username passed client validation and
/// came back 400 from the server. Field-presence parity could never catch that -- both types had a
/// Username property. Anything that reads a published contract (OpenAPI/Swagger schemas, generated
/// clients, UI form validation) is built from these annotations, so a mismatch is a lie to consumers.
/// </para>
/// <para>
/// Discovery is reflective rather than a hand-maintained pair list: any NEW duplicated DTO added later
/// is picked up automatically instead of silently escaping the guard.
/// </para>
/// </summary>
public class ValidationConstraintParityTests
{
    /// <summary>Assembly holding the server-side endpoint request/response contracts.</summary>
    private const string ServerAssemblyName = "Fdw.Web.Api";

    /// <summary>
    /// Type+property pairs whose constraints intentionally differ, each with the reason. Keyed
    /// "TypeName.PropertyName". Anything not listed here must match on both sides.
    /// </summary>
    private static readonly Dictionary<string, string> AllowedDifferences = new(StringComparer.Ordinal)
    {
        // (empty -- every known divergence was reconciled. Add an entry ONLY with a stated reason.)
    };

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SharedFieldsHaveMatchingValidationConstraints()
    {
        var mismatches = new List<string>();

        foreach (var (typeName, serverType, clientType) in DuplicatedContractTypes())
        {
            var serverProps = Constraints(serverType);
            var clientProps = Constraints(clientType);

            foreach (var jsonName in serverProps.Keys.Intersect(clientProps.Keys, StringComparer.OrdinalIgnoreCase))
            {
                var server = serverProps[jsonName];
                var client = clientProps[jsonName];

                if (server == client)
                {
                    continue;
                }

                if (AllowedDifferences.ContainsKey($"{typeName}.{jsonName}"))
                {
                    continue;
                }

                mismatches.Add(
                    $"{typeName}.{jsonName}: server[{server}] != client[{client}]  " +
                    $"({serverType.FullName} vs {clientType.Assembly.GetName().Name}::{clientType.FullName})");
            }
        }

        mismatches.ShouldBeEmpty(
            "Client and server contracts disagree on validation rules for the same field. " +
            "A consumer built from one contract will be rejected by the other:" +
            Environment.NewLine + string.Join(Environment.NewLine, mismatches));
    }

    /// <summary>
    /// Finds every type name declared BOTH in the server endpoint assembly and in some other FDW
    /// assembly, i.e. the duplicated request/response contracts.
    /// </summary>
    private static IEnumerable<(string TypeName, Type Server, Type Client)> DuplicatedContractTypes()
    {
        var assemblies = LoadFdwAssemblies();

        var serverTypes = assemblies
            .Where(a => string.Equals(a.GetName().Name, ServerAssemblyName, StringComparison.Ordinal))
            .SelectMany(SafeTypes)
            .Where(IsContractType)
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        foreach (var candidate in assemblies
            .Where(a => !string.Equals(a.GetName().Name, ServerAssemblyName, StringComparison.Ordinal))
            .SelectMany(SafeTypes)
            .Where(IsContractType))
        {
            if (serverTypes.TryGetValue(candidate.Name, out var server) && server != candidate)
            {
                yield return (candidate.Name, server, candidate);
            }
        }
    }

    private static bool IsContractType(Type t)
        => t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsGenericTypeDefinition
           && t.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.CanRead && p.CanWrite);

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        // Why: a partially-loadable assembly must not take the whole guard down -- take what resolved.
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static IReadOnlyList<Assembly> LoadFdwAssemblies()
    {
        // Why load from disk rather than AppDomain.CurrentDomain.GetAssemblies(): a referenced
        // assembly is not loaded until something touches it, so the in-memory list would silently
        // omit exactly the contracts nothing has referenced yet.
        var loaded = new List<Assembly>();

        foreach (var path in Directory.GetFiles(AppContext.BaseDirectory, "Fdw.*.dll"))
        {
            try
            {
                loaded.Add(Assembly.LoadFrom(path));
            }
            catch (BadImageFormatException)
            {
                // Native/mixed-mode artifact -- not a managed contract assembly.
            }
            catch (FileLoadException)
            {
                // Already loaded under a different context; the loaded copy is in the list already.
            }
        }

        return loaded;
    }

    /// <summary>
    /// Reduces a type's DataAnnotations to a normalized per-field constraint set, so that
    /// <c>[StringLength(50, MinimumLength = 3)]</c> and <c>[MaxLength(50)] [MinLength(3)]</c> --
    /// which mean the same thing -- compare as equal.
    /// </summary>
    private static Dictionary<string, string> Constraints(Type type)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite))
        {
            var parts = new List<string>();

            if (p.GetCustomAttribute<RequiredAttribute>() is not null)
            {
                parts.Add("required");
            }

            int? min = p.GetCustomAttribute<MinLengthAttribute>()?.Length;
            int? max = p.GetCustomAttribute<MaxLengthAttribute>()?.Length;

            if (p.GetCustomAttribute<StringLengthAttribute>() is { } sl)
            {
                max ??= sl.MaximumLength;

                if (sl.MinimumLength > 0)
                {
                    min ??= sl.MinimumLength;
                }
            }

            if (min is not null)
            {
                parts.Add($"min={min.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (max is not null)
            {
                parts.Add($"max={max.Value.ToString(CultureInfo.InvariantCulture)}");
            }

            if (p.GetCustomAttribute<EmailAddressAttribute>() is not null)
            {
                parts.Add("email");
            }

            if (p.GetCustomAttribute<RangeAttribute>() is { } range)
            {
                parts.Add($"range={range.Minimum}..{range.Maximum}");
            }

            if (p.GetCustomAttribute<RegularExpressionAttribute>() is { } regex)
            {
                parts.Add($"regex={regex.Pattern}");
            }

            parts.Sort(StringComparer.Ordinal);

            result[JsonName(p)] = string.Join(",", parts);
        }

        return result;
    }

    private static string JsonName(PropertyInfo p)
        => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name;
}
