using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Fdw.Conventions.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// FDW073: no limit written in code (FDW-7059). A limit is configuration (Mike, 2026-11-04: "if there is a question about 'should there be a max number
/// of' or 'should there be a limit on' ... the answer is always make it configurable"). It reports a public <c>int</c> or <c>long</c> constant named
/// <c>Max</c>, <c>Maximum</c>, <c>Default</c> or <c>Limit</c> (alone or followed by a capital), except:
/// <list type="bullet">
/// <item>a <c>[PlatformDefault]</c> constant: the declared default a host changes at registration, where a stated configuration value wins;</item>
/// <item>a <c>[FormatLimit("reason")]</c> constant: a number a protocol or file format fixes, with the reason;</item>
/// <item>a <c>[ConfigurationPending("FDW-n")]</c> constant: a cap found before the rule, with the filed story that makes it configuration.</item>
/// </list>
/// An empty reason and a key that is not a project key and a number are reported too.
/// </summary>
/// <remarks>
/// Why constants and not every number: a const named as a limit is how a cap is written down to be shared, and every one the FDW-7059 sweep found was one.
/// A literal compared in a guard clause is out of scope here. A compiler extension (generator, analyzer, code fix) is left out: it runs inside the compiler
/// and cannot reference the attributes.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoCodedLimitAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a limit written in code.
    /// </summary>
    public const string DiagnosticId = "FDW073";

    private const string PlatformDefaultName = "Fdw.Conventions.PlatformDefaultAttribute";
    private const string FormatLimitName = "Fdw.Conventions.FormatLimitAttribute";
    private const string ConfigurationPendingName = "Fdw.Conventions.ConfigurationPendingAttribute";

    private static readonly ImmutableArray<string> LimitPrefixes = ["Maximum", "Max", "Default", "Limit"];

    private static readonly Regex ItemKey = new("^[A-Z][A-Z0-9]*-[0-9]+$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Limit written in code",
        "{0}",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A limit is configuration, never code. Declare its default in the service type option's DeclaredDefaults, read it at Register with DeclaredSetting.ConfiguredCount (a host changes it with ServiceTypeDefaults.Override, and a value the host's configuration states wins), and hand the configured value to what checks it (see DefaultMessagingServiceType and Messaging:Default:UiFeedback). A number a protocol or file format fixes is marked [FormatLimit(\"reason\")]; a cap with a filed story to make it configuration is marked [ConfigurationPending(\"FDW-n\")] until the story removes the mark.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            if (CompilerExtensions.IsCompilerExtension(start.Compilation))
                return;

            var platformDefault = start.Compilation.GetTypeByMetadataName(PlatformDefaultName);
            var formatLimit = start.Compilation.GetTypeByMetadataName(FormatLimitName);
            var pending = start.Compilation.GetTypeByMetadataName(ConfigurationPendingName);
            start.RegisterSymbolAction(symbolContext => AnalyzeField(symbolContext, platformDefault, formatLimit, pending), SymbolKind.Field);
        });
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol? platformDefault, INamedTypeSymbol? formatLimit, INamedTypeSymbol? pending)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (!field.IsConst || field.DeclaredAccessibility != Accessibility.Public || !IsWholeNumber(field.Type) || !IsLimitName(field.Name))
            return;

        if (AttributeOf(field, platformDefault) is not null)
            return;

        if (AttributeOf(field, formatLimit) is { } format)
        {
            if (string.IsNullOrWhiteSpace(FirstArgument(format)))
                Report(context, field, $"'{field.Name}' is marked [FormatLimit] with no reason: name the protocol or file format that fixes {Shown(field)} and how it fixes it");

            return;
        }

        if (AttributeOf(field, pending) is { } marked)
        {
            if (FirstArgument(marked) is not { } key || !ItemKey.IsMatch(key))
                Report(context, field, $"'{field.Name}' is marked [ConfigurationPending] with '{FirstArgument(marked)}', which is not an item key: name the filed story that makes it configuration, such as FDW-7103");

            return;
        }

        Report(context, field,
            $"'{field.Name}' is a limit written in code ({Shown(field)}). A limit is configuration: declare its default in the service type option's DeclaredDefaults, read it at Register with DeclaredSetting.ConfiguredCount (a host changes it with ServiceTypeDefaults.Override, and a value the host's configuration states wins), and hand the configured value to what checks it (see DefaultMessagingServiceType). If a protocol or file format fixes the number, mark it [FormatLimit(\"which format, and why\")]");
    }

    private static bool IsWholeNumber(ITypeSymbol type) => type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Int64;

    // Why the next character must be a capital or nothing: Maxwell or Defaults is not a limit, MaxRows and Max are.
    private static bool IsLimitName(string name) =>
        LimitPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal) && (name.Length == prefix.Length || char.IsUpper(name[prefix.Length])));

    private static AttributeData? AttributeOf(ISymbol symbol, INamedTypeSymbol? attribute) =>
        attribute is null ? null : symbol.GetAttributes().FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute));

    private static string? FirstArgument(AttributeData attribute) =>
        attribute.ConstructorArguments.Length > 0 ? attribute.ConstructorArguments[0].Value as string : null;

    // Only an int or long constant reaches here, so its value always formats.
    private static string Shown(IFieldSymbol field) => string.Format(CultureInfo.InvariantCulture, "{0}", field.ConstantValue);

    private static void Report(SymbolAnalysisContext context, IFieldSymbol field, string message)
    {
        foreach (var location in field.Locations.Where(location => location.IsInSource))
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, message));
    }
}
