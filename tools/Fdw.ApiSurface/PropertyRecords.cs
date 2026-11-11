using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the record of one property: its accessors, whether a setter is init-only, and whether it is required.</summary>
internal static class PropertyRecords
{
    public static void Write(SurfaceContext context, OwnerInfo owner, PropertyDefinitionHandle handle)
    {
        MetadataReader reader = context.Reader;
        PropertyDefinition property = reader.GetPropertyDefinition(handle);
        PropertyAccessors accessors = property.GetAccessors();
        MethodDefinition? getter = accessors.Getter.IsNil ? null : reader.GetMethodDefinition(accessors.Getter);
        MethodDefinition? setter = accessors.Setter.IsNil ? null : reader.GetMethodDefinition(accessors.Setter);
        string? getAccess = getter is null ? null : Access.OfMethod(getter.Value.Attributes);
        string? setAccess = setter is null ? null : Access.OfMethod(setter.Value.Attributes);
        string? access = Access.Higher(getAccess, setAccess);
        if (access is null || (string.Equals(access, "protected", StringComparison.Ordinal) && !owner.Inheritable))
        {
            return;
        }

        MethodSignature<string> signature = property.DecodeSignature(context.Provider, owner.Names);
        string indexer = signature.ParameterTypes.Length == 0 ? string.Empty : "[" + string.Join(",", signature.ParameterTypes) + "]";
        string key = owner.Key + "::" + reader.GetString(property.Name) + indexer;
        MethodDefinition source = Either(getter, setter);
        MethodAttributes attributes = source.Attributes;
        string required = CustomAttributes.Has(reader, property.GetCustomAttributes(), "System.Runtime.CompilerServices.RequiredMemberAttribute")
            ? "required"
            : string.Empty;
        string setWord = setter is null ? string.Empty : Accessor(IsInitOnly(context, owner, setter.Value) ? "init" : "set", setAccess, access);
        string getWord = getter is null ? string.Empty : Accessor("get", getAccess, access);
        string detail = Words.Join("property", access, MemberModifiers.Of(attributes), required, getWord, setWord, "->", signature.ReturnType);
        context.Lines.Add(context.Framework, "MEMBER", key, detail);
    }

    private static MethodDefinition Either(MethodDefinition? getter, MethodDefinition? setter)
    {
        if (getter is { } first)
        {
            return first;
        }

        if (setter is { } second)
        {
            return second;
        }

        throw new InvalidOperationException("A property with neither accessor reached the writer.");
    }

    private static bool IsInitOnly(SurfaceContext context, OwnerInfo owner, MethodDefinition setter)
    {
        return setter.DecodeSignature(context.Provider, owner.Names).ReturnType.Contains("IsExternalInit", StringComparison.Ordinal);
    }

    private static string Accessor(string word, string? accessorAccess, string propertyAccess)
    {
        return string.Equals(accessorAccess, propertyAccess, StringComparison.Ordinal) ? word : word + "(" + accessorAccess + ")";
    }
}
