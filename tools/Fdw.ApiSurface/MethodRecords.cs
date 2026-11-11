using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace Fdw.ApiSurface;

/// <summary>Writes the record of one method or constructor: its key is its name and parameter types, its detail the rest.</summary>
internal static class MethodRecords
{
    public static void Write(SurfaceContext context, OwnerInfo owner, MethodDefinitionHandle handle)
    {
        MetadataReader reader = context.Reader;
        MethodDefinition method = reader.GetMethodDefinition(handle);
        string? access = Access.OfMethod(method.Attributes);
        string name = reader.GetString(method.Name);
        if (access is null || (string.Equals(access, "protected", StringComparison.Ordinal) && !owner.Inheritable) || name.StartsWith('<') || string.Equals(name, ".cctor", StringComparison.Ordinal))
        {
            return;
        }

        IReadOnlyList<string> generics = TypeKeys.ParameterNames(reader, method.GetGenericParameters());
        MethodSignature<string> signature = method.DecodeSignature(context.Provider, new GenericNames(owner.Names.Type, generics));
        string suffix = generics.Count == 0 ? string.Empty : "<" + string.Join(",", generics) + ">";
        string key = owner.Key + "::" + name + suffix + "(" + string.Join(",", signature.ParameterTypes) + ")";
        string kind = string.Equals(name, ".ctor", StringComparison.Ordinal) ? "ctor" : "method";
        string detail = Words.Join(kind, access, MemberModifiers.Of(method.Attributes), ParameterFlags(reader, method), "->", signature.ReturnType);
        context.Lines.Add(context.Framework, "MEMBER", key, detail);
    }

    private static string ParameterFlags(MetadataReader reader, MethodDefinition method)
    {
        List<int> outs = [];
        List<int> optionals = [];
        foreach (ParameterHandle handle in method.GetParameters())
        {
            Parameter parameter = reader.GetParameter(handle);
            if (parameter.SequenceNumber == 0)
            {
                continue;
            }

            if (parameter.Attributes.HasFlag(ParameterAttributes.Out))
            {
                outs.Add(parameter.SequenceNumber);
            }

            if ((parameter.Attributes & (ParameterAttributes.Optional | ParameterAttributes.HasDefault)) != ParameterAttributes.None)
            {
                optionals.Add(parameter.SequenceNumber);
            }
        }

        return Words.Join(outs.Count == 0 ? string.Empty : "out(" + string.Join(",", outs) + ")",
            optionals.Count == 0 ? string.Empty : "opt(" + string.Join(",", optionals) + ")");
    }
}
