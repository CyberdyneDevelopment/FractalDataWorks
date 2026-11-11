using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the records of one public type: TYPE, BASE, IMPL and CONSTRAINT, then hands its members on.</summary>
internal static class TypeRecords
{
    public static void Write(SurfaceContext context, TypeDefinitionHandle handle)
    {
        MetadataReader reader = context.Reader;
        TypeDefinition definition = reader.GetTypeDefinition(handle);
        if (!Access.IsVisible(reader, definition) || reader.GetString(definition.Name).StartsWith('<'))
        {
            return;
        }

        GenericNames names = new(TypeKeys.ParameterNames(reader, definition.GetGenericParameters()), []);
        string key = TypeKeys.Key(reader, handle, definition);
        bool isInterface = (definition.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
        string? baseName = definition.BaseType.IsNil ? null : HandleNames.Entity(reader, definition.BaseType, context.Provider, names);

        context.Lines.Add(context.Framework, "TYPE", key, TypeKeys.Detail(definition, isInterface, baseName));
        if (baseName is not null && !isInterface)
        {
            context.Lines.Add(context.Framework, "BASE", key, baseName);
        }

        WriteInterfaces(context, definition, key, names);
        WriteConstraints(context, definition, key);
        bool inheritable = isInterface || !definition.Attributes.HasFlag(TypeAttributes.Sealed);
        MemberRecords.Write(context, definition, new OwnerInfo(key, names, inheritable));
    }

    private static void WriteInterfaces(SurfaceContext context, TypeDefinition definition, string key, GenericNames names)
    {
        foreach (InterfaceImplementationHandle handle in definition.GetInterfaceImplementations())
        {
            EntityHandle implemented = context.Reader.GetInterfaceImplementation(handle).Interface;
            string name = HandleNames.Entity(context.Reader, implemented, context.Provider, names);
            context.Lines.Add(context.Framework, "IMPL", key + " : " + name, string.Empty);
        }
    }

    private static void WriteConstraints(SurfaceContext context, TypeDefinition definition, string key)
    {
        MetadataReader reader = context.Reader;
        GenericNames names = new(TypeKeys.ParameterNames(reader, definition.GetGenericParameters()), []);
        foreach (GenericParameterHandle handle in definition.GetGenericParameters())
        {
            GenericParameter parameter = reader.GetGenericParameter(handle);
            List<string> constraints = Flags(parameter.Attributes);
            constraints.AddRange(parameter.GetConstraints()
                .Select(c => HandleNames.Entity(reader, reader.GetGenericParameterConstraint(c).Type, context.Provider, names))
                .Order(System.StringComparer.Ordinal));
            if (constraints.Count > 0)
            {
                context.Lines.Add(context.Framework, "CONSTRAINT", key + " " + reader.GetString(parameter.Name), string.Join(", ", constraints));
            }
        }
    }

    private static List<string> Flags(GenericParameterAttributes attributes)
    {
        List<string> flags = [];
        if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
        {
            flags.Add("class");
        }

        if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
        {
            flags.Add("struct");
        }

        if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
        {
            flags.Add("new()");
        }

        return flags;
    }
}
