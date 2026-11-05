using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// FDW055: no fallbacks. A <c>??</c> or <c>??=</c> never falls back to a value that could be a real one (Mike,
/// 2026-11-04). It reports every coalesce except:
/// <list type="bullet">
/// <item><c>?? throw</c>, a null logger (<c>NullLogger&lt;T&gt;.Instance</c>, <c>NullLogger.Instance</c>,
/// <c>NullLoggerFactory.Instance</c>), <c>?? string.Empty</c> and <c>?? []</c>, and for a dictionary, whose interfaces
/// <c>[]</c> does not convert to, <c>ImmutableDictionary&lt;K, V&gt;.Empty</c>;</item>
/// <item><c>?? DBNull.Value</c>, which is how ADO.NET writes SQL NULL for a null value, not a substitute for it;</item>
/// <item>a static <c>NotFound</c>, a collection's null object, which every consumer checks for and refuses;</item>
/// <item><c>??=</c> on a field or local, which sets it once (lazy init, keep-first);</item>
/// <item>a coalesce whose type is a result or a message (<c>ProblemA() ?? ProblemB()</c>, every operand a failure);</item>
/// <item>a field of a <c>[PlaceholderText]</c> class, or a member marked <c>[PlatformDefault]</c>;</item>
/// <item>the same field of another instance (<c>child.X ?? parent.X</c>, <c>request.X ?? existing.X</c>): an unset
/// setting inherits its parent's value, and an update that leaves a field out keeps the stored one;</item>
/// <item>any coalesce in a member Mike approved with <c>[ApprovedFallback("reason")]</c>.</item>
/// </list>
/// A property initializer on a connection, schedule, scheduler or notification configuration that is a valid value (<c>= 30</c>,
/// <c>= true</c>, <c>= "localhost"</c>) is reported too: a row states that value, and the initializer is the fallback a row that leaves it
/// out would get. A <c>[PlatformDefault]</c> member, such as a driver's documented default, is allowed.
/// A default message (<c>x.CurrentMessage ?? ...</c>) is reported whatever its right side, and <c>""</c> and the
/// long spellings of an empty collection are reported with a code fix to <c>string.Empty</c> and <c>[]</c>.
/// </summary>
/// <remarks>
/// Why operations: whether a coalesce is a failure chain, a placeholder or a platform default depends on the types and
/// attributes of what it references, which only the semantic model knows (FDW-3580).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NoFallbackAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a fallback value.
    /// </summary>
    public const string DiagnosticId = "FDW055";

    /// <summary>
    /// The diagnostic property that names the code fix: <see cref="StringEmptyFix"/> or <see cref="EmptyCollectionFix"/>.
    /// </summary>
    public const string FixProperty = "Fix";

    /// <summary>
    /// The <see cref="FixProperty"/> value for a <c>""</c> fallback, rewritten to <c>string.Empty</c>.
    /// </summary>
    public const string StringEmptyFix = "StringEmpty";

    /// <summary>
    /// The <see cref="FixProperty"/> value for an empty collection spelled long, rewritten to <c>[]</c>.
    /// </summary>
    public const string EmptyCollectionFix = "EmptyCollection";

    private const string ApprovedFallbackName = "Fdw.Conventions.ApprovedFallbackAttribute";
    private const string PlaceholderTextName = "Fdw.Conventions.PlaceholderTextAttribute";
    private const string PlatformDefaultName = "Fdw.Conventions.PlatformDefaultAttribute";

    private const string FailLoud = "fail loud with a MessageLogging message and a failed result";

    // The configurations whose values a row states (FDW-3580): an initializer on one of them is the value a row that leaves it out gets.
    private static readonly ImmutableArray<string> StatedConfigurations =
        ["IConnectionImplementationConfiguration", "IScheduleImplementationConfiguration", "ISchedulerImplementationConfiguration", "INotificationImplementationConfiguration"];

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Fallback value",
        "{0}",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A ?? never falls back to a value that could be a real one. Allowed: '?? throw', a null logger, '?? string.Empty', '?? []', '??=' on a field or local, a failure chain whose type is a result or message, a [PlaceholderText] constant, a [PlatformDefault] member, the same field of another instance (a parent or stored value), DBNull.Value, a static NotFound sentinel, ImmutableDictionary.Empty, and a member Mike approved with [ApprovedFallback(\"reason\")]. A default message is never allowed.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            // Why compiler extensions are out of scope (FDW-3580): FDW055 guards values a running system reads. A source generator,
            // analyzer or code fix runs inside the compiler; its choices are conventions of the code it emits, reviewed as code.
            if (IsCompilerExtension(start.Compilation))
                return;

            var known = new KnownSymbols(start.Compilation);
            start.RegisterOperationAction(
                operationContext => AnalyzeCoalesce(operationContext, known),
                OperationKind.Coalesce);
            start.RegisterOperationAction(
                operationContext => AnalyzePropertyInitializer(operationContext, known),
                OperationKind.PropertyInitializer);
            start.RegisterOperationAction(
                operationContext => AnalyzeCoalesceAssignment(operationContext, known),
                OperationKind.CoalesceAssignment);
        });
    }

    private static void AnalyzeCoalesce(OperationAnalysisContext context, KnownSymbols known)
    {
        var coalesce = (ICoalesceOperation)context.Operation;
        if (IsFailureCarrier(coalesce.Type, known))
            return;

        Evaluate(context, known, coalesce.Value, coalesce.WhenNull);
    }

    private static void AnalyzeCoalesceAssignment(OperationAnalysisContext context, KnownSymbols known)
    {
        var assignment = (ICoalesceAssignmentOperation)context.Operation;
        switch (assignment.Target)
        {
            case IFieldReferenceOperation:
            case ILocalReferenceOperation:
                return;
            case IParameterReferenceOperation parameter when !IsApproved(context, out _):
                Report(context, $"'??=' on parameter '{parameter.Parameter.Name}' defaults an argument: {FailLoud}");
                return;
        }

        Evaluate(context, known, assignment.Target, assignment.Value);
    }

    private static readonly ImmutableArray<string> CompilerExtensionAttributes =
        ["Microsoft.CodeAnalysis.GeneratorAttribute", "Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzerAttribute", "Microsoft.CodeAnalysis.CodeFixes.ExportCodeFixProviderAttribute"];

    /// <summary>Whether the compilation is a compiler extension: it references Roslyn and declares a generator, analyzer or code fix.</summary>
    private static bool IsCompilerExtension(Compilation compilation) =>
        compilation.ReferencedAssemblyNames.Any(reference => string.Equals(reference.Name, "Microsoft.CodeAnalysis", StringComparison.Ordinal)) &&
        DeclaresExtension(compilation.Assembly.GlobalNamespace);

    private static bool DeclaresExtension(INamespaceSymbol ns) =>
        ns.GetTypeMembers().Any(type => type.GetAttributes().Any(a =>
            a.AttributeClass is { } attribute && CompilerExtensionAttributes.Contains(attribute.ToDisplayString(), StringComparer.Ordinal))) ||
        ns.GetNamespaceMembers().Any(DeclaresExtension);

    /// <summary>Reports a valid-value initializer on a property of a configuration whose values a row states.</summary>
    private static void AnalyzePropertyInitializer(OperationAnalysisContext context, KnownSymbols known)
    {
        var initializer = (IPropertyInitializerOperation)context.Operation;
        // Why IsCurrent and IsDeleted pass: they are the store's versioning flags on the row, not a value the configuration states.
        if (initializer.InitializedProperties.FirstOrDefault() is not { } property || !IsStatedConfiguration(property.ContainingType) ||
            property.Name is "IsCurrent" or "IsDeleted")
            return;

        var value = Unwrap(initializer.Value);
        if (!IsValidValueLiteral(value) || IsDeclaredText(value, known) || IsApproved(context, out _))
            return;

        Report(context, $"'{Shown(value)}' is assumed for '{property.Name}': a configuration row states it, so remove the initializer and {FailLoud} where it is missing; a driver's documented default is a [PlatformDefault] member");
    }

    private static bool IsStatedConfiguration(INamedTypeSymbol type) =>
        type.AllInterfaces.Any(i => StatedConfigurations.Contains(i.Name, StringComparer.Ordinal));

    /// <summary>
    /// A literal or constant that is a real value: true, a number other than 0, or text that is not empty. false, 0 and "" are the CLR's own.
    /// A constant counts so that only a [PlatformDefault] one (checked by the caller) passes.
    /// </summary>
    private static bool IsValidValueLiteral(IOperation value) =>
        value is ILiteralOperation or IFieldReferenceOperation { Field.IsConst: true } &&
        value.ConstantValue is { HasValue: true, Value: { } constant } && constant switch
        {
            bool flag => flag,
            string text => text.Length > 0,
            int number => number != 0,
            long number => number != 0,
            double number => number != 0,
            decimal number => number != 0,
            _ => false,
        };

    /// <summary>Reports the fallback <paramref name="whenNull"/> for <paramref name="value"/> unless a rule allows it.</summary>
    private static void Evaluate(OperationAnalysisContext context, KnownSymbols known, IOperation value, IOperation whenNull)
    {
        var right = Unwrap(whenNull);
        if (right is IThrowOperation || IsNullLogger(right) || IsStringEmpty(right) || IsEmptyCollectionExpression(right) ||
            IsImmutableEmptyWithoutBrackets(right) || IsImmutableDictionaryEmpty(right) || IsDbNull(right) || IsNotFoundSentinel(right))
            return;

        if (IsApproved(context, out var reason))
        {
            if (string.IsNullOrWhiteSpace(reason))
                Report(context, "[ApprovedFallback] needs the reason Mike gave for this fallback");
            return;
        }

        if (IsMessage(Unwrap(value)))
        {
            Report(context, $"'{Shown(right)}' is a default message: give the thing you consume a message, or make a new one, instead of a fallback");
            return;
        }

        if (IsDeclaredText(right, known) || IsSameFieldOfAnotherInstance(Unwrap(value), right))
            return;

        ReportValue(context, right);
    }

    /// <summary>Reports a right side no rule allowed, with the message and code fix its shape calls for.</summary>
    private static void ReportValue(OperationAnalysisContext context, IOperation right)
    {
        if (right is ILiteralOperation { ConstantValue: { HasValue: true, Value: string text } })
        {
            if (text.Length == 0)
            {
                Report(context, "'\"\"' is written string.Empty", StringEmptyFix);
                return;
            }

            Report(context, $"'{Shown(right)}' is a bare literal: placeholder text is a const in a [PlaceholderText] class, and a real value must {FailLoud}");
            return;
        }

        if (IsEmptyDictionaryCreation(right))
        {
            Report(context, $"'{Shown(right)}' is the empty dictionary: write ImmutableDictionary<K, V>.Empty, since [] does not convert to a dictionary interface");
            return;
        }

        if (IsEmptyCollectionSpelledLong(right))
        {
            Report(context, $"'{Shown(right)}' is the empty collection: write []", EmptyCollectionFix);
            return;
        }

        Report(context, $"'{Shown(right)}' is a fallback value: {FailLoud}, or have Mike approve it with [ApprovedFallback(\"reason\")]");
    }

    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
            operation = conversion.Operand;
        return operation;
    }

    private static bool IsFailureCarrier(ITypeSymbol? type, KnownSymbols known) =>
        type is not null && (IsOrImplements(type, known.GenericResult) || IsOrImplements(type, known.GenericMessage));

    private static bool IsOrImplements(ITypeSymbol type, INamedTypeSymbol? target) =>
        target is not null &&
        (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, target) ||
         type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, target)));

    /// <summary>Whether <paramref name="right"/> is NullLogger&lt;T&gt;.Instance, NullLogger.Instance or NullLoggerFactory.Instance.</summary>
    // Why: a null logger is often cast to the interface ('(ILogger)NullLogger.Instance') so the two sides of '??' share a
    // type. The cast is explicit, so Unwrap leaves it; look through it here, since the value is still the null logger.
    private static bool IsNullLogger(IOperation right) =>
        ReferencedMember(right is IConversionOperation conversion ? conversion.Operand : right) is { Name: "Instance" } member &&
        (string.Equals(member.ContainingType.Name, "NullLogger", StringComparison.Ordinal) ||
         string.Equals(member.ContainingType.Name, "NullLoggerFactory", StringComparison.Ordinal));

    private static bool IsStringEmpty(IOperation right) =>
        right is IFieldReferenceOperation { Field: { Name: "Empty", ContainingType.SpecialType: SpecialType.System_String } };

    private static bool IsEmptyCollectionExpression(IOperation right) =>
        right is ICollectionExpressionOperation { Elements.Length: 0 };

    /// <summary>Array.Empty, Enumerable.Empty, ImmutableArray/ImmutableList Empty, or a new collection with no arguments and no elements.</summary>
    private static bool IsEmptyCollectionSpelledLong(IOperation right) => !IsEmptyDictionaryCreation(right) && right switch
    {
        IInvocationOperation { TargetMethod.Name: "Empty", Arguments.Length: 0 } invocation =>
            invocation.TargetMethod.ContainingType.Name is "Array" or "Enumerable",
        IFieldReferenceOperation { Field.Name: "Empty" } field =>
            IsImmutableEmpty(field) && HasCollectionBuilder(field.Field.ContainingType),
        IObjectCreationOperation { Arguments.Length: 0 } creation =>
            IsEmptyInitializer(creation.Initializer) && IsCollection(creation.Type),
        IArrayCreationOperation array =>
            array.Initializer is null or { ElementValues.Length: 0 } &&
            array.DimensionSizes.All(size => size.ConstantValue is { HasValue: true, Value: 0 }),
        _ => false,
    };

    private static bool IsImmutableDictionaryEmpty(IOperation right) =>
        right is IFieldReferenceOperation { Field.Name: "Empty" } field &&
        field.Field.ContainingType.Name is "ImmutableDictionary" or "ImmutableSortedDictionary" &&
        string.Equals(field.Field.ContainingType.ContainingNamespace?.ToDisplayString(), "System.Collections.Immutable", StringComparison.Ordinal);

    private static bool IsDbNull(IOperation right) =>
        right is IFieldReferenceOperation { Field: { Name: "Value", IsStatic: true } } field &&
        string.Equals(field.Field.ContainingType.ToDisplayString(), "System.DBNull", StringComparison.Ordinal);

    /// <summary>A static member named NotFound: the null object a type collection or registry returns for an unknown name.</summary>
    private static bool IsNotFoundSentinel(IOperation right) =>
        ReferencedMember(right) is { IsStatic: true, Name: "NotFound" };

    /// <summary>A new dictionary with no elements, whatever comparer it is given: an empty dictionary has no keys to compare.</summary>
    private static bool IsEmptyDictionaryCreation(IOperation right) =>
        right is IObjectCreationOperation creation &&
        IsEmptyInitializer(creation.Initializer) &&
        creation.Arguments.All(argument => argument.Value.Type?.AllInterfaces.Any(i => i.Name is "IEqualityComparer") == true ||
                                           argument.Value.Type?.Name is "IEqualityComparer") &&
        creation.Type?.AllInterfaces.Any(i => i.Name is "IDictionary" or "IReadOnlyDictionary" && i.IsGenericType) == true;

    private static bool IsImmutableEmpty(IFieldReferenceOperation field) =>
        field.Field.ContainingType.Name is "ImmutableArray" or "ImmutableList";

    /// <summary>
    /// ImmutableArray/ImmutableList Empty where the type has no [CollectionBuilder] (an older System.Collections.Immutable,
    /// as netstandard2.0 generators reference): [] does not compile there, so Empty is the empty marker itself.
    /// </summary>
    private static bool IsImmutableEmptyWithoutBrackets(IOperation right) =>
        right is IFieldReferenceOperation { Field.Name: "Empty" } field &&
        IsImmutableEmpty(field) &&
        !HasCollectionBuilder(field.Field.ContainingType);

    private static bool HasCollectionBuilder(INamedTypeSymbol type) =>
        type.OriginalDefinition.GetAttributes().Any(a =>
            string.Equals(a.AttributeClass?.ToDisplayString(), "System.Runtime.CompilerServices.CollectionBuilderAttribute", StringComparison.Ordinal));

    private static bool IsEmptyInitializer(IObjectOrCollectionInitializerOperation? initializer) =>
        initializer is null || initializer.Initializers.Length == 0;

    private static bool IsCollection(ITypeSymbol? type) =>
        type is not null &&
        type.SpecialType != SpecialType.System_String &&
        type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable);

    /// <summary>Whether the left side is a message: a string member named Message or ending in Message (CurrentMessage).</summary>
    private static bool IsMessage(IOperation value) =>
        value.Type?.SpecialType == SpecialType.System_String &&
        ReferencedMember(value) is { } member &&
        member.Name.EndsWith("Message", StringComparison.Ordinal);

    /// <summary>Whether <paramref name="right"/> names a [PlaceholderText] constant or a [PlatformDefault] member.</summary>
    private static bool IsDeclaredText(IOperation right, KnownSymbols known)
    {
        var member = ReferencedMember(right);
        if (member is null)
            return false;

        return (member is IFieldSymbol { IsConst: true } && HasAttribute(member.ContainingType, known.PlaceholderText)) ||
               HasAttribute(member, known.PlatformDefault);
    }

    /// <summary>
    /// Whether <paramref name="value"/> and <paramref name="right"/> read the same-named field or property of two
    /// different instances: the parent's or the stored record's value of the field left unset.
    /// </summary>
    /// <remarks>
    /// Why by name: the two sides are often different types (a request and the stored configuration, a node and its
    /// parent's snapshot). Mike, 2026-11-04 (FDW-3580): "just fall back to the parent value in each one", and an update
    /// that omits a field keeps the stored value. A different field of the same record is a substitute, not the same
    /// value, and stays reported.
    /// </remarks>
    private static bool IsSameFieldOfAnotherInstance(IOperation value, IOperation right) =>
        ReferencedMember(value) is { } left &&
        ReferencedMember(right) is { } fallback &&
        string.Equals(left.Name, fallback.Name, StringComparison.Ordinal) &&
        Instance(value) is { } leftInstance &&
        Instance(right) is { } rightInstance &&
        !string.Equals(leftInstance.Syntax.ToString(), rightInstance.Syntax.ToString(), StringComparison.Ordinal);

    private static IOperation? Instance(IOperation operation) => operation switch
    {
        IFieldReferenceOperation field => field.Instance,
        IPropertyReferenceOperation property => property.Instance,
        _ => null,
    };

    private static ISymbol? ReferencedMember(IOperation operation) => operation switch
    {
        IFieldReferenceOperation field => field.Field,
        IPropertyReferenceOperation property => property.Property,
        _ => null,
    };

    private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attribute) =>
        attribute is not null &&
        symbol.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attribute));

    /// <summary>
    /// Whether the member holding the coalesce, or a member enclosing it (a local function's method, an accessor's
    /// property), carries [ApprovedFallback]; <paramref name="reason"/> is its reason.
    /// </summary>
    private static bool IsApproved(OperationAnalysisContext context, out string reason)
    {
        for (var symbol = context.ContainingSymbol; symbol is not null and not INamedTypeSymbol; symbol = symbol.ContainingSymbol)
        {
            var found = ApprovalOnMemberOrAssociated(symbol);
            if (found is not null)
            {
                reason = found;
                return true;
            }
        }

        reason = string.Empty;
        return false;
    }

    /// <summary>The [ApprovedFallback] reason on <paramref name="symbol"/>, or on the property or event an accessor belongs to.</summary>
    private static string? ApprovalOnMemberOrAssociated(ISymbol symbol)
    {
        var onMember = Approval(symbol);
        if (onMember is not null)
            return onMember;

        return symbol is IMethodSymbol { AssociatedSymbol: { } associated } ? Approval(associated) : null;
    }

    /// <summary>The reason on the symbol's [ApprovedFallback], an empty string when it has none, or null without the attribute.</summary>
    private static string? Approval(ISymbol symbol)
    {
        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a => string.Equals(a.AttributeClass?.ToDisplayString(), ApprovedFallbackName, StringComparison.Ordinal));
        if (attribute is null)
            return null;

        return attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is string reason
            ? reason
            : string.Empty;
    }

    private static string Shown(IOperation right)
    {
        var text = right.Syntax.ToString();
        return text.Length > 60 ? text.Substring(0, 60) + "..." : text;
    }

    private static void Report(OperationAnalysisContext context, string message, string? fix = null)
    {
        var properties = fix is null
            ? ImmutableDictionary<string, string?>.Empty
            : ImmutableDictionary<string, string?>.Empty.Add(FixProperty, fix);

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), properties, message));
    }

    /// <summary>The attribute and interface symbols FDW055 looks for, resolved once per compilation.</summary>
    private sealed class KnownSymbols(Compilation compilation)
    {
        public INamedTypeSymbol? GenericResult { get; } = compilation.GetTypeByMetadataName("Fdw.Results.IGenericResult");

        public INamedTypeSymbol? GenericMessage { get; } = compilation.GetTypeByMetadataName("Fdw.Messages.IGenericMessage");

        public INamedTypeSymbol? PlaceholderText { get; } = compilation.GetTypeByMetadataName(PlaceholderTextName);

        public INamedTypeSymbol? PlatformDefault { get; } = compilation.GetTypeByMetadataName(PlatformDefaultName);
    }
}
