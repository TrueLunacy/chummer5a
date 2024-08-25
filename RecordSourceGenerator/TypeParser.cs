using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using Theraot.Collections.Specialized;

namespace RecordSourceGenerator
{
    // todo: 95% sure an array type won't use the XmlRecord name override, fix that
    public static class TypeParser
    {
        public static Model? FromType(INamedTypeSymbol type)
        {
            if (!type!.IsRecord)
                return null; // diagnostic
            if (type.BaseType?.MetadataName is not nameof(Object) or nameof(ValueType))
                return null;

            var ns = type.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
            var classid = type.Name;

            var attr = type.GetAttributes().Single(a => a.AttributeClass?.Name == SourceGenerator.RecordAttr);
            string elementName = classid;
            if (attr.NamedArguments.Any(na => na.Key == SourceGenerator.ElementNameProp))
            {
                var prop = attr.NamedArguments.Single(na => na.Key == SourceGenerator.ElementNameProp);
                elementName = (string)prop.Value.Value!;
            }

            // records have two constructors by default, and multiple can be defined
            // we want the primary constructor, which cannot be determined
            // we can eliminate the copy constructor, otherwise, if there's multiple constructors
            // we must return a diagnostic
            var constructors = type.InstanceConstructors
                .Where(c => !(c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, type)))
                .ToArray();
            IMethodSymbol? constructor;
            Debug.Assert(constructors.Length != 0); // how would that even
            if (constructors.Length > 1)
            {
                constructor = constructors
                    .Where(c => c.GetAttributes().Any(a => a.AttributeClass?.MetadataName == SourceGenerator.ConstructorAttr))
                    .SingleOrDefault();
                if (constructor is null) // should be reported by diagnostic
                    return null;
            }
            else
            {
                constructor = constructors.Single();
            }

            var paramz = constructor.Parameters
                .Where(t => !t.GetAttributes().Any(t => t.AttributeClass?.Name == SourceGenerator.NoParseAttr))
                .Select(p =>
                {
                    ITypeSymbol innertype;
                    bool immutableArrayOf = false;
                    if (p.Type.Name == "ImmutableArray")
                    {
                        immutableArrayOf = true;
                        innertype = ((INamedTypeSymbol)p.Type).TypeArguments[0];
                    }
                    else
                    {
                        innertype = p.Type;
                    }
                    string name = p.Name;
                    string fulltype = innertype.ToString();
                    ParamClass klass;
                    if (innertype.AllInterfaces.Any(i => i.Name == "IParsable" || i.MetadataName == "ISpanParsable"))
                    {
                        // it would be very unusual if it implemented IParsable/ISpanParsable for a different type
                        klass = ParamClass.ParsableType;
                    }
                    else if (innertype.BaseType?.MetadataName == "Enum")
                    {
                        klass = ParamClass.EnumType;
                    }
                    else if (innertype.GetAttributes().Any(a => a.AttributeClass?.MetadataName == SourceGenerator.RecordAttr))
                    {
                        klass = ParamClass.KnownXmlRecordType;
                    }
                    else
                    {
                        klass = ParamClass.UnknownType;
                    }

                    //var proptype = p.ContainingType.GetMembers()
                    //    .Single(m => m.Name == p.Name);

                    var propattr = p.GetAttributes().SingleOrDefault(a => a.AttributeClass?.Name
                        is SourceGenerator.ElementAttr or SourceGenerator.AttributeAttr or SourceGenerator.PresenceAttr);

                    string xmlName;
                    string? xmlChildName;
                    XmlPosition pos;
                    if (propattr is not null)
                    {
                        pos = propattr.AttributeClass!.Name switch
                        {
                            SourceGenerator.AttributeAttr => XmlPosition.Attribute,
                            SourceGenerator.PresenceAttr => XmlPosition.Presence,
                            SourceGenerator.ElementAttr => XmlPosition.Element,
                            _ => throw new InvalidOperationException()
                        };
                        if (propattr.NamedArguments.Any(p => p.Key
                            is SourceGenerator.AttrNameProp or SourceGenerator.ElementNameProp))
                        {
                            var arg = propattr.NamedArguments.Single(p => p.Key
                                is SourceGenerator.AttrNameProp or SourceGenerator.ElementNameProp);
                            xmlName = (string)arg.Value.Value!;
                        }
                        else
                        {
                            xmlName = p.Name;
                        }
                        if (immutableArrayOf)
                        {
                            if (propattr.NamedArguments.Any(a => a.Key == SourceGenerator.ArrayProp))
                            {
                                var arg = propattr.NamedArguments.Single(p => p.Key
                                is SourceGenerator.ArrayProp);
                                xmlChildName = (string)arg.Value.Value!;
                            }
                            else
                            {
                                xmlChildName = innertype.Name;
                            }
                        }
                        else
                        {
                            xmlChildName = null;
                        }
                    }
                    else
                    {
                        xmlName = p.Name;
                        xmlChildName = immutableArrayOf ? innertype.Name : null;
                        pos = XmlPosition.Element;
                    }

                    return new Parameter(
                        ParameterName: p.Name,
                        XmlName: xmlName,
                        XmlChildName: xmlChildName,
                        FullyQualifiedType: fulltype,
                        TypeIsValueType: innertype.IsValueType,
                        ContainingType: type.Name,
                        Position: pos,
                        Class: klass);
                });

            paramz = paramz.Concat(type.GetAttributes()
                .Where(a => a.AttributeClass?.Name == SourceGenerator.SpecialHandlingAttr)
                .Select(a =>
                {
                    var elementname = a.ConstructorArguments.Single().Value?.ToString();
                    Debug.Assert(elementname is not null);
                    return new Parameter(
                        ParameterName: null,
                        XmlName: elementname!,
                        XmlChildName: null,
                        FullyQualifiedType: null,
                        TypeIsValueType: false,
                        ContainingType: type.Name,
                        Position: XmlPosition.Element,
                        Class: ParamClass.TotalOverride
                    );
                }));

            return new Model(
                Namespace: ns,
                ClassIdentifier: classid,
                XmlElementName: elementName,
                IsStruct: type.IsValueType,
                Params: paramz.ToImmutableArray());

        }
    }
}
