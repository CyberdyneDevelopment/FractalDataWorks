using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>
/// Extracts records from XML payloads using XPath expressions.
/// Strips all XML namespaces as preprocessing, then evaluates RecordSelector to find repeating elements,
/// and evaluates each FieldMapping's PhysicalFieldName as relative XPath per element.
/// All extracted values are raw string? — type coercion is the transform chain's responsibility.
/// </summary>
[TypeOption(typeof(DataSetSourceMapperTypes), "XPath")]
public sealed class XPathDataSetSourceMapper : DataSetSourceMapperTypeBase
{
    // Why: TypeOptions are singletons discovered by source generation — they have no DI-injected logger.
    // NullLogger ensures MessageLogging methods can create IGenericMessage instances for results.
    // The message content is still returned in the IGenericResult for the caller to observe.
    private static readonly ILogger Logger = NullLogger.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="XPathDataSetSourceMapper"/> class.
    /// </summary>
    public XPathDataSetSourceMapper()
        : base(
            id: 1,
            name: "XPath",
            displayName: "XPath",
            description: "Extracts records from XML payloads using XPath expressions.",
            category: "Mapper")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IReadOnlyList<Dictionary<string, object?>>>> MapRecords(
        DataSetSourceMapperContext context,
        CancellationToken cancellationToken = default)
    {
        XElement root;
        try
        {
            root = ResolvePayload(context.Payload);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                DataSetSourceMapperLog.PayloadParseFailed(Logger, Name, ex.Message));
        }

        // Why: Stripping namespaces as preprocessing simplifies all downstream XPath evaluation.
        // A future NamespaceAwareXPath TypeOption can handle namespace-sensitive scenarios.
        StripNamespaces(root);

        IEnumerable<XElement> recordElements;
        try
        {
            var evaluated = root.XPathEvaluate(context.RecordSelector);

            // Why: XPathEvaluate returns IEnumerable for node-set results, so we cast and filter.
            recordElements = evaluated is IEnumerable<object> enumerable
                ? enumerable.OfType<XElement>().ToList()
                : Array.Empty<XElement>();
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                DataSetSourceMapperLog.RecordSelectorFailed(Logger, context.RecordSelector, Name, ex.Message));
        }

        var orderedMappings = context.FieldMappings
            .OrderBy(m => m.Ordinal)
            .ToList();

        var records = new List<Dictionary<string, object?>>();

        foreach (var element in recordElements)
        {
            var record = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var mapping in orderedMappings)
            {
                var logicalName = mapping.LogicalFieldName;

                if (string.IsNullOrEmpty(mapping.PhysicalFieldName))
                {
                    // Why: Empty PhysicalFieldName signals constant injection — the transform chain
                    // handles it via Constant (id 500) or Parameter (id 501) transforms.
                    record[logicalName] = null;
                    continue;
                }

                try
                {
                    var value = EvaluateFieldXPath(element, mapping.PhysicalFieldName);
                    record[logicalName] = value;
                }
                catch (Exception ex)
                {
                    return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                        DataSetSourceMapperLog.FieldExtractionFailed(Logger, mapping.PhysicalFieldName, logicalName, Name, ex.Message));
                }

                // Apply transform chain (ascending ordinal order)
                foreach (var step in mapping.Transforms.OrderBy(s => s.Ordinal))
                {
                    var typeOption = DataTransformerTypes.ByName(step.TransformType);
                    if (typeOption == DataTransformerTypes.NotFound)
                    {
                        return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                            DataSetSourceMapperLog.TransformTypeNotFound(Logger, step.TransformType, logicalName));
                    }

                    if (typeOption is not FieldTransformerTypeBase fieldTransformer)
                    {
                        return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                            DataSetSourceMapperLog.TransformTypeNotFieldTransformer(Logger, step.TransformType, logicalName));
                    }

                    var transformContext = new FieldTransformContext
                    {
                        OperatingDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        ExecutionTimestamp = DateTimeOffset.UtcNow,
                        CurrentRecord = record,
                        Parameters = step.Parameters,
                        CancellationToken = cancellationToken
                    };

                    var stepResult = await fieldTransformer.Transform(record[logicalName], transformContext, cancellationToken).ConfigureAwait(false);
                    if (!stepResult.IsSuccess)
                    {
                        return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Failure(
                            DataSetSourceMapperLog.TransformStepFailed(Logger, step.TransformType, step.Ordinal, logicalName,
                                stepResult.CurrentMessage ?? "transform failed"));
                    }

                    record[logicalName] = stepResult.Value;
                }
            }

            records.Add(record);
        }

        return GenericResult<IReadOnlyList<Dictionary<string, object?>>>.Success(records);
    }

    /// <summary>
    /// Resolves the payload to an XElement, handling both string and XElement input types.
    /// </summary>
    private static XElement ResolvePayload(object payload)
    {
        return payload switch
        {
            XElement xElement => xElement,
            string xmlString => XElement.Parse(xmlString),
            _ => throw new InvalidOperationException(
                $"XPath mapper expects XElement or string payload, got {payload.GetType().Name}")
        };
    }

    /// <summary>
    /// Removes all namespace declarations and prefixes from the element tree.
    /// </summary>
    private static void StripNamespaces(XElement root)
    {
        foreach (var element in root.DescendantsAndSelf())
        {
            // Why: Setting the name to local name only removes the namespace prefix.
            element.Name = element.Name.LocalName;

            var attributes = element.Attributes()
                .Where(a => a.IsNamespaceDeclaration || a.Name.Namespace != XNamespace.None)
                .ToList();

            foreach (var attr in attributes)
            {
                if (attr.IsNamespaceDeclaration)
                {
                    attr.Remove();
                }
                else
                {
                    // Why: Non-declaration attributes with a namespace get their namespace stripped.
                    var value = attr.Value;
                    attr.Remove();
                    element.SetAttributeValue(attr.Name.LocalName, value);
                }
            }
        }
    }

    /// <summary>
    /// Evaluates a relative XPath expression against an element and returns the string value.
    /// </summary>
    private static string? EvaluateFieldXPath(XElement element, string xpath)
    {
        var result = element.XPathEvaluate(xpath);

        if (result is IEnumerable<object> nodes)
        {
            var first = nodes.FirstOrDefault();
            return first switch
            {
                XElement xElement => xElement.Value,
                XAttribute xAttribute => xAttribute.Value,
                XText xText => xText.Value,
                null => null,
                _ => first.ToString()
            };
        }

        // Why: XPathEvaluate returns scalar types (string, double, bool) for non-node-set expressions.
        return result?.ToString();
    }
}
