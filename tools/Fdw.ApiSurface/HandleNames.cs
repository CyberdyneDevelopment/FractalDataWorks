using System;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Full names of types named by a handle: namespace, nesting with +, no generic arity (an instantiation adds its arguments).</summary>
internal static class HandleNames
{
    public static string Definition(MetadataReader reader, TypeDefinitionHandle handle)
    {
        TypeDefinition definition = reader.GetTypeDefinition(handle);
        string name = WithoutArity(reader.GetString(definition.Name));
        TypeDefinitionHandle declaring = definition.GetDeclaringType();
        if (!declaring.IsNil)
        {
            return Definition(reader, declaring) + "+" + name;
        }

        return Qualified(reader.GetString(definition.Namespace), name);
    }

    public static string Reference(MetadataReader reader, TypeReferenceHandle handle)
    {
        TypeReference reference = reader.GetTypeReference(handle);
        string name = WithoutArity(reader.GetString(reference.Name));
        if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            return Reference(reader, (TypeReferenceHandle)reference.ResolutionScope) + "+" + name;
        }

        return Qualified(reader.GetString(reference.Namespace), name);
    }

    public static string Entity(MetadataReader reader, EntityHandle handle, SignatureTextProvider provider, GenericNames names)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => Definition(reader, (TypeDefinitionHandle)handle),
            HandleKind.TypeReference => Reference(reader, (TypeReferenceHandle)handle),
            HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(provider, names),
            _ => throw new System.InvalidOperationException($"A type was named by a {handle.Kind} handle, which this tool does not read."),
        };
    }

    private static string Qualified(string ns, string name)
    {
        return ns.Length == 0 ? name : ns + "." + name;
    }

    private static string WithoutArity(string name)
    {
        int tick = name.IndexOf('`', StringComparison.Ordinal);
        return tick < 0 ? name : name[..tick];
    }
}
