using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>The key a type has in the surface (its name with generic parameters and variance) and the text of its kind and modifiers.</summary>
internal static class TypeKeys
{
    public static IReadOnlyList<string> ParameterNames(MetadataReader reader, GenericParameterHandleCollection handles)
    {
        return [.. handles.Select(handle => reader.GetString(reader.GetGenericParameter(handle).Name))];
    }

    public static string Key(MetadataReader reader, TypeDefinitionHandle handle, TypeDefinition definition)
    {
        string name = HandleNames.Definition(reader, handle);
        IReadOnlyList<string> parameters = [.. definition.GetGenericParameters().Select(h => Parameter(reader, h))];
        return parameters.Count == 0 ? name : name + "<" + string.Join(",", parameters) + ">";
    }

    public static string Detail(TypeDefinition definition, bool isInterface, string? baseName)
    {
        if (isInterface)
        {
            return "interface";
        }

        string kind = baseName switch
        {
            "System.Enum" => "enum",
            "System.ValueType" => "struct",
            "System.MulticastDelegate" => "delegate",
            _ => "class",
        };
        bool isAbstract = definition.Attributes.HasFlag(TypeAttributes.Abstract);
        bool isSealed = definition.Attributes.HasFlag(TypeAttributes.Sealed);
        if (!string.Equals(kind, "class", StringComparison.Ordinal))
        {
            return kind;
        }

        if (isAbstract && isSealed)
        {
            return "class static";
        }

        return isAbstract ? "class abstract" : isSealed ? "class sealed" : "class";
    }

    private static string Parameter(MetadataReader reader, GenericParameterHandle handle)
    {
        GenericParameter parameter = reader.GetGenericParameter(handle);
        string variance = (parameter.Attributes & GenericParameterAttributes.VarianceMask) switch
        {
            GenericParameterAttributes.Covariant => "out ",
            GenericParameterAttributes.Contravariant => "in ",
            _ => string.Empty,
        };
        return variance + reader.GetString(parameter.Name);
    }
}
