using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Analyzers;

/// <summary>
/// FDW058: an endpoint never declares PUT. Creating, attaching or placing something is POST; changing something that
/// exists is PATCH (Mike, 2026-11-05: "we always always always use post and patch definitely not put"; FDW-4125).
/// </summary>
/// <remarks>
/// Reported: a call to FastEndpoints' <c>Put(...)</c>, a <c>Verbs(...)</c> argument that is the constant text PUT, and any
/// reference to <c>FastEndpoints.Http.PUT</c>, which also catches a verb handed to <c>Verbs</c> through a field or a
/// record (a relay that stores its verb). Outbound HTTP to another system (<c>System.Net.Http.HttpMethod.Put</c>, an HTTP
/// connection's method option) is not a route this API serves and is not reported.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PutRouteAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for an endpoint that declares PUT.
    /// </summary>
    public const string DiagnosticId = "FDW058";

    private const string FastEndpointsNamespace = "FastEndpoints";
    private const string HttpEnumName = "FastEndpoints.Http";
    private const string PutText = "PUT";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "An endpoint declares PUT",
        "'{0}' declares a PUT route: use POST to create, attach or place something, or PATCH to change something that exists",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The API takes no PUT. A call that creates, attaches or places something is POST; a call that changes something that already exists is PATCH. Outbound HTTP to another system is not affected.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (!IsFastEndpoints(method.ContainingNamespace))
            return;

        if (string.Equals(method.Name, "Put", StringComparison.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), "Put(...)"));
            return;
        }

        if (!string.Equals(method.Name, "Verbs", StringComparison.Ordinal))
            return;

        foreach (var argument in invocation.Arguments)
        {
            foreach (var value in Values(argument.Value))
            {
                if (value.ConstantValue is { HasValue: true, Value: string text } &&
                    string.Equals(text, PutText, StringComparison.OrdinalIgnoreCase))
                    context.ReportDiagnostic(Diagnostic.Create(Rule, value.Syntax.GetLocation(), "Verbs(\"PUT\")"));
            }
        }
    }

    // Why every reference to Http.PUT and not only Verbs' arguments: a verb can reach Verbs through a field or a record
    // (EtlRelay(Http.PUT, ...) then Verbs(Relay.Verb)), where the argument no longer names PUT.
    private static void AnalyzeFieldReference(OperationAnalysisContext context)
    {
        var reference = (IFieldReferenceOperation)context.Operation;
        var field = reference.Field;
        if (string.Equals(field.Name, PutText, StringComparison.Ordinal) &&
            string.Equals(field.ContainingType?.ToDisplayString(), HttpEnumName, StringComparison.Ordinal))
            context.ReportDiagnostic(Diagnostic.Create(Rule, reference.Syntax.GetLocation(), "Http.PUT"));
    }

    // A params argument arrives as an array creation; its elements are the verbs written.
    private static ImmutableArray<IOperation> Values(IOperation value) =>
        value is IArrayCreationOperation { Initializer: { } initializer }
            ? initializer.ElementValues
            : [value];

    private static bool IsFastEndpoints(INamespaceSymbol? ns) =>
        ns is not null && string.Equals(ns.ToDisplayString(), FastEndpointsNamespace, StringComparison.Ordinal);
}
