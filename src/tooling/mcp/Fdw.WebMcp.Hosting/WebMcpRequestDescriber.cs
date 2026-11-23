using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Describes a tool's request type to JSON Schema, recursively: scalars, lists of them and objects with members of their own.
/// </summary>
/// <remarks>
/// <para>
/// Lives apart from <see cref="WebMcpToolPlan"/> because it is the one place that walks a type, and it keeps three rules the
/// plan would otherwise have to carry: how deep a request may nest (the setting <c>WebMcpToolSchemaMaxDepth</c>, handed in,
/// never assumed), that a type reaching itself has no finite description, and that a member no schema shape describes is
/// skipped and logged, never guessed at.
/// </para>
/// <para>
/// Depth counts the lists and objects on the way down. The request is depth 0; <c>Attendees</c> (a list) is 1 and its item (an
/// object) is 2. A scalar adds nothing.
/// </para>
/// </remarks>
internal sealed class WebMcpRequestDescriber
{
    private readonly string _toolName;
    private readonly int _maxDepth;
    private readonly bool _carriesBody;
    private readonly ILogger _logger;
    private readonly List<Type> _chain = [];

    private WebMcpRequestDescriber(string toolName, int maxDepth, bool carriesBody, ILogger logger)
    {
        _toolName = toolName;
        _maxDepth = maxDepth;
        _carriesBody = carriesBody;
        _logger = logger;
    }

    /// <summary>Describes the members of a tool's request type.</summary>
    /// <param name="requestType">The endpoint's request type.</param>
    /// <param name="toolName">The tool, for what is logged and refused.</param>
    /// <param name="maxDepth">How many lists and objects a member may nest; the setting <c>WebMcpToolSchemaMaxDepth</c>.</param>
    /// <param name="carriesBody">Whether the call carries a JSON body. Only a body can carry a list or an object, so a tool without one describes scalars alone.</param>
    /// <param name="logger">Receives one entry per member mapped or skipped.</param>
    /// <returns>The members in declaration order, or the refusal of a request that nests too deep or reaches itself.</returns>
    internal static IGenericResult<IReadOnlyList<WebMcpToolInput>> Describe(
        Type requestType,
        string toolName,
        int maxDepth,
        bool carriesBody,
        ILogger logger)
        => new WebMcpRequestDescriber(toolName, maxDepth, carriesBody, logger).Members(requestType, string.Empty, depth: 0, isRequest: true);

    private IGenericResult<IReadOnlyList<WebMcpToolInput>> Members(Type type, string path, int depth, bool isRequest)
    {
        _chain.Add(type);
        var members = new List<WebMcpToolInput>();

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanWrite))
        {
            var memberPath = path.Length == 0 ? property.Name : path + "." + property.Name;
            var described = Describe(property.PropertyType, memberPath, depth);
            if (described.IsFailure)
            {
                return described.ToNewResult<IReadOnlyList<WebMcpToolInput>>();
            }

            if (described.Value is not { } shape)
            {
                continue;
            }

            members.Add(shape with { Name = property.Name, Required = IsRequired(property, shape, isRequest) });
        }

        _chain.RemoveAt(_chain.Count - 1);
        return GenericResult<IReadOnlyList<WebMcpToolInput>>.Success(members);
    }

    /// <summary>The shape of one member, or no value when no schema shape describes it.</summary>
    private IGenericResult<WebMcpToolInput?> Describe(Type clrType, string path, int depth)
    {
        var (jsonType, format) = WebMcpToolPlan.JsonSchemaOf(clrType);
        if (jsonType.Length > 0)
        {
            WebMcpLog.SchemaPropertyMapped(_logger, _toolName, path, jsonType, format);
            return GenericResult<WebMcpToolInput?>.Success(new WebMcpToolInput(string.Empty, jsonType, format));
        }

        var shape = _carriesBody
            ? Composite(Nullable.GetUnderlyingType(clrType) is { } inner ? inner : clrType, path, depth)
            : GenericResult<WebMcpToolInput?>.Success(null);
        if (shape.IsSuccess && shape.Value is null)
        {
            WebMcpLog.SchemaPropertySkipped(_logger, _toolName, path, clrType.ToString());
        }

        return shape;
    }

    private IGenericResult<WebMcpToolInput?> Composite(Type clrType, string path, int depth)
    {
        if (ItemTypeOf(clrType) is { } itemType)
        {
            return List(itemType, path, depth);
        }

        return IsObject(clrType) ? Object(clrType, path, depth) : GenericResult<WebMcpToolInput?>.Success(null);
    }

    private IGenericResult<WebMcpToolInput?> List(Type itemType, string path, int depth)
    {
        if (depth + 1 > _maxDepth)
        {
            return GenericResult<WebMcpToolInput?>.Failure(WebMcpLog.SchemaTooDeep(_logger, _toolName, path, _maxDepth));
        }

        var item = Describe(itemType, path + "[]", depth + 1);
        if (item.IsFailure || item.Value is not { } itemShape)
        {
            // An item no schema shape describes leaves a list nobody can describe, so the list is skipped with it.
            return item;
        }

        WebMcpLog.SchemaPropertyMapped(_logger, _toolName, path, "array", string.Empty);
        return GenericResult<WebMcpToolInput?>.Success(new WebMcpToolInput(string.Empty, "array", string.Empty, Items: itemShape));
    }

    private IGenericResult<WebMcpToolInput?> Object(Type objectType, string path, int depth)
    {
        if (_chain.Contains(objectType))
        {
            return GenericResult<WebMcpToolInput?>.Failure(WebMcpLog.SchemaCycle(_logger, _toolName, path, objectType.ToString()));
        }

        if (depth + 1 > _maxDepth)
        {
            return GenericResult<WebMcpToolInput?>.Failure(WebMcpLog.SchemaTooDeep(_logger, _toolName, path, _maxDepth));
        }

        var members = Members(objectType, path, depth + 1, isRequest: false);
        if (members.IsFailure || members.Value is not { } described)
        {
            return members.ToNewResult<WebMcpToolInput?>();
        }

        WebMcpLog.SchemaPropertyMapped(_logger, _toolName, path, "object", string.Empty);
        return GenericResult<WebMcpToolInput?>.Success(new WebMcpToolInput(string.Empty, "object", string.Empty, Properties: described));
    }

    /// <summary>
    /// Whether a member must be present. A member nested in an object, or a list or object member of the request, is required
    /// by the <c>required</c> modifier or by being a value type that cannot be null; a scalar of the request itself never is
    /// (only its path parameters are), because requiring it would refuse a call that works today.
    /// </summary>
    private static bool IsRequired(PropertyInfo property, WebMcpToolInput shape, bool isRequest)
    {
        var declaredRequired = property.GetCustomAttribute<RequiredMemberAttribute>() is not null;
        if (isRequest)
        {
            return declaredRequired && (shape.Items is not null || shape.Properties is not null);
        }

        return declaredRequired
            || (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null);
    }

    /// <summary>The item type of a list, or no type when the type is not one this describes as a list.</summary>
    /// <remarks>
    /// A dictionary enumerates key/value pairs, which JSON writes as an object, and a byte array is written as base64 text, so
    /// neither is a list on the wire.
    /// </remarks>
    private static Type? ItemTypeOf(Type type)
    {
        if (type == typeof(byte[]) || type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetArrayRank() == 1 ? type.GetElementType() : null;
        }

        var enumerable = type.GetInterfaces().Append(type)
            .FirstOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        if (enumerable is null)
        {
            return null;
        }

        var item = enumerable.GetGenericArguments()[0];
        return item.IsGenericType && item.GetGenericTypeDefinition() == typeof(KeyValuePair<,>) ? null : item;
    }

    /// <summary>
    /// Whether a type is an object this describes by its members: a concrete class that is not the framework's. A framework type
    /// (a <c>Uri</c>, a JSON node) is written by a converter of its own, so describing it by its properties would misdescribe it.
    /// </summary>
    private static bool IsObject(Type type)
        => type is { IsClass: true, IsAbstract: false }
            && type != typeof(object)
            && type.Namespace is { } space
            && !space.StartsWith("System", StringComparison.Ordinal)
            && !space.StartsWith("Microsoft", StringComparison.Ordinal);
}
