using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RecordSourceGenerator
{
    // todo: restructure
    // todo: create partial method code fix
    // todo: if you have an inner element with the same end as the outer, the code fucking explodes
    // todo: attr-based type converters
    [Generator]
    public class SourceGenerator : IIncrementalGenerator
    {
        private const string AttrNs = "RecordSourceGenerator.Generated";
        public const string RecordAttr = "XmlRecordAttribute";
        public const string ConstructorAttr = "XmlRecordConstructorAttribute";
        public const string ElementAttr = "XmlAsElementAttribute";
        public const string AttributeAttr = "XmlAsAttributeAttribute";
        public const string PresenceAttr = "XmlAsPresenceAttribute";

        public const string ConverterAttr = "XmlConverterAttribute";

        public const string ElementNameProp = "ElementName";
        public const string AttrNameProp = "AttributeName";
        public const string ArrayProp = "ArrayElementName";

        private const string GeneratedAttrFullName = AttrNs + "." + RecordAttr;

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }

            // this creates the attribute in the local project
            // TODO: use local namespace?
            context.RegisterPostInitializationOutput(static pic => pic.AddSource("XmlRecordAttribute.cs", SourceText.From($$"""
            namespace {{AttrNs}};
             
            [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
            internal sealed class {{RecordAttr}} : Attribute
            {
                public {{RecordAttr}}()
                {
                }

                public string? {{ElementNameProp}} { get; set; }
            }

            [System.AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
            internal sealed class {{ConstructorAttr}} : Attribute
            {
                public {{ConstructorAttr}}()
                {
                }
            }

            [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
            internal sealed class {{ElementAttr}} : Attribute
            {
                public {{ElementAttr}}()
                {
                }

                public string? {{ElementNameProp}} { get; set; }
                public string? {{ArrayProp}} { get; set; }
            }

            [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
            internal sealed class {{PresenceAttr}} : Attribute
            {
                public {{PresenceAttr}}()
                {
                }
            
                public string? {{ElementNameProp}} { get; set; }
            }

            [System.AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
            internal sealed class {{AttributeAttr}} : Attribute
            {
                public {{AttributeAttr}}()
                {
                }

                public string? {{AttrNameProp}} { get; set; }
            }

            [System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
            internal sealed class {{ConverterAttr}}<T> : Attribute
            {
                public {{ConverterAttr}}()
                {
                }
            }
            """, Encoding.UTF8)));

            var pipeline = context.SyntaxProvider.ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: GeneratedAttrFullName,
                predicate: static (node, ct) => node is RecordDeclarationSyntax,
                transform: static (ctx, ct) =>
                {
                    var klass = ctx.TargetSymbol as INamedTypeSymbol;
                    Debug.Assert(klass is not null);
                    return TypeParser.FromType(klass!);
                }
            );

            context.RegisterSourceOutput(pipeline, static (ctx, model) =>
            {
                if (model is null) return; // something invalid, don't generate

                HashSet<string> partialmethods = new();
                IndentingStringBuilder isb = new();
                // overall design, we need to generate two methods
                // public static T Read(XmlReader reader, T? defaultValue = default);
                // public void Write(XmlWriter writer);

                // Read is constructed as two big switches, one for attrs., one for elements
                // What we do with the value depends on the type
                // 1. Known basic type (string, int, etc.) Deserialize directly
                // 2. Enum type: Deserialize from string
                // 3. Known XmlRecord type: Call Read()
                // 4. Other: Emit a partial method
                isb.WriteLine($$"""
                #nullable enable
                #pragma warning disable CS1522
                using System;
                using System.Xml;
                using System.Reflection;
                using System.Diagnostics;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                using System.Runtime.CompilerServices;

                namespace {{model.Namespace ?? AttrNs}};

                public partial record {{model.ClassIdentifier}}
                """);
                using (var classbrace = isb.Brace())
                {
                    EmitReadMethod(model, isb, partialmethods);
                    EmitWriteMethod(model, isb, partialmethods);

                    foreach (var str in partialmethods)
                        isb.WriteLine(str);
                }
                ctx.AddSource($"{model.ClassIdentifier}.g.cs", SourceText.From(isb.ToString(), Encoding.UTF8));
            });

        }

        private static void EmitWriteMethod(Model model, IndentingStringBuilder isb, ISet<string> partialmethods)
        {
            isb.WriteLine($"public void Write(XmlWriter writer, string elementName = \"{model.XmlElementName}\")");
            using var methodbrace = isb.Brace();
            isb.WriteLine($"writer.WriteStartElement(elementName);");
            foreach (var attr in model.Params.Where(p => p.Position == XmlPosition.Attribute))
            {
                Debug.Assert(!attr.ImmutableArrayOf);
                Debug.Assert(attr.Class is ParamClass.EnumType or ParamClass.ParsableType);
                IDisposable? brace = null;
                // this isn't the usual IDisposable, but it's fine
                // if we exception, there's no harm in letting the dispose go free
                if (!attr.TypeIsValueType)
                {
                    isb.WriteLine($"if ({attr.ParameterName} is not null)");
                    brace = isb.Brace();
                }
                isb.WriteLine($"writer.WriteAttributeString(\"{attr.XmlName}\", {attr.ParameterName}.ToString());");
                brace?.Dispose();
            }
            foreach (var prop in model.Params.Where(p => p.Position != XmlPosition.Attribute))
            {
                if (prop.Position == XmlPosition.Presence)
                {
                    isb.WriteLine($$"""
                    if ({{prop.ParameterName}})
                    {
                        writer.WriteStartElement("{{prop.XmlName}}");
                        writer.WriteEndElement();
                    }
                    """);
                    continue; // early break from this
                }
                IDisposable? brace = null;
                // this isn't the usual IDisposable, but it's fine
                // if we exception, there's no harm in letting the dispose go free
                if (!prop.TypeIsValueType && !prop.ImmutableArrayOf)
                {
                    isb.WriteLine($"if ({prop.ParameterName} is not null)");
                    brace = isb.Brace();
                }
                if (prop.HasPartialMethod)
                    partialmethods.Add(prop.GetWritePartialMethod());
                if (prop.ImmutableArrayOf)
                {
                    isb.WriteLine($"if (!{prop.ParameterName}.IsDefaultOrEmpty)");
                    using var ifbrace = isb.Brace();
                    isb.WriteLine($$"""
                    writer.WriteStartElement("{{prop.XmlName}}");
                    foreach (var _elementProbablyNotPropertyNameString in {{prop.ParameterName}})
                    """);
                    using var foreachbrace = isb.Brace();
                    IDisposable? foreachelembrace = null;
                    if (!prop.TypeIsValueType)
                    {
                        isb.WriteLine($"if (_elementProbablyNotPropertyNameString is not null)");
                        foreachelembrace = isb.Brace();
                    }
                    if (prop.TryParseable || prop.StringType)
                    {
                        isb.WriteLine($$"""
                        writer.WriteStartElement("{{prop.XmlChildName}}");
                        writer.WriteValue(_elementProbablyNotPropertyNameString.ToString());
                        writer.WriteEndElement();
                        """);
                    }
                    else
                    {
                        isb.WriteLine(prop.WriteStatement("writer", prop.XmlChildName!, "_elementProbablyNotPropertyNameString"));
                    }
                    foreachelembrace?.Dispose();
                    isb.WriteLine("writer.WriteEndElement();");
                }
                else
                {
                    if (prop.TryParseable || prop.StringType)
                    {
                        isb.WriteLine($$"""
                        writer.WriteStartElement("{{prop.XmlName}}");
                        writer.WriteValue({{prop.ParameterName}}.ToString());
                        writer.WriteEndElement();
                        """);
                    }
                    else
                    {
                        isb.WriteLine(prop.WriteStatement("writer", prop.XmlName));
                    }
                }
                brace?.Dispose();
            }
            isb.WriteLine("writer.WriteEndElement();");
        }

        private static void EmitReadMethod(Model model, IndentingStringBuilder isb, ISet<string> partialmethods)
        {
            isb.WriteLine($"public static {model.ClassIdentifier} Read(XmlReader reader, {model.ClassIdentifier}? defaultValue = default, string elementName = \"{model.XmlElementName}\")");
            using var methodbrace = isb.Brace(); // disposed at end of method
            isb.WriteLine($$"""
            reader.MoveToContent();
            if (reader.NodeType != XmlNodeType.Element || reader.Name != elementName)
                throw new InvalidOperationException($"Invalid XML - expected {elementName} but got {reader.Name} on type {reader.NodeType}");
            """);
            if (model.IsStruct)
            {
                isb.WriteLine($"{model.ClassIdentifier} returnObject = defaultValue;");
            }
            else
            {
                isb.WriteLine($"""
                {model.ClassIdentifier} returnObject = defaultValue == null
                    ? ({model.ClassIdentifier})RuntimeHelpers.GetUninitializedObject(typeof({model.ClassIdentifier}))
                    : new {model.ClassIdentifier}(defaultValue);
                """);
            }
            foreach (var arrtype in model.Params.Where(p => p.ImmutableArrayOf))
            {
                //ImmutableArray<int>.Empty
                isb.WriteLine($"if (returnObject.{arrtype.ParameterName}.IsDefaultOrEmpty)");
                using var brace = isb.Brace();
                isb.WriteLine($$"""
                MethodInfo field = typeof({{model.ClassIdentifier}})
                    .GetProperty(nameof({{arrtype.ParameterName}}))!
                    .GetSetMethod()!;
                field.Invoke(returnObject, new object?[] { ImmutableArray<{{arrtype.FullyQualifiedType}}>.Empty });
                """);
            }
            isb.WriteLine("if (reader.HasAttributes)");
            using (var attrif = isb.Brace())
            {
                isb.WriteLine("while (reader.MoveToNextAttribute())");
                using (var attrwhile = isb.Brace())
                {
                    isb.WriteLine("switch (reader.Name)");
                    using var attrswitch = isb.Brace();
                    foreach (var param in model.Params.Where(p => p.Position == XmlPosition.Attribute))
                    {
                        isb.WriteLine($"case \"{param.XmlName}\":");
                        using (var casebrace = isb.Brace())
                        {
                            Debug.Assert(!param.ImmutableArrayOf);
                            if (param.HasPartialMethod)
                                partialmethods.Add(param.GetParsePartialMethod());
                            if (param.StringType)
                            {
                                isb.WriteLine($$"""
                                MethodInfo field = typeof({{model.ClassIdentifier}})
                                    .GetProperty(nameof({{param.ParameterName}}))!
                                    .GetSetMethod()!;
                                field.Invoke(returnObject, new object?[] { reader.Value });
                                """);

                            }
                            else if (param.TryParseable)
                            {
                                isb.WriteLine($$"""
                                if ({{param.GetTryParse("v", "reader.Value")}})
                                {
                                    MethodInfo field = typeof({{model.ClassIdentifier}})
                                        .GetProperty(nameof({{param.ParameterName}}))!
                                        .GetSetMethod()!;
                                    field.Invoke(returnObject, new object?[] { v });
                                }
                                """);
                            }
                            else
                            {
                                isb.WriteLine($$"""
                                {{param.GetValueAssignment("v", "reader", $"defaultValue{(model.IsStruct ? "" : "?")}.{param.ParameterName}")}}
                                MethodInfo field = typeof({{model.ClassIdentifier}})
                                    .GetProperty(nameof({{param.ParameterName}}))!
                                    .GetSetMethod()!;
                                field.Invoke(returnObject, new object?[] { v });
                                """);
                            }
                        }
                        isb.WriteLine("break;");
                        isb.WriteLine("""
                        default:
                            reader.Read();
                            break;
                        """);
                    }
                }
                isb.WriteLine("reader.MoveToElement();");
            }
            isb.WriteLine("Debug.Assert(reader.Name == elementName);");
            isb.WriteLine($"reader.Read();");
            isb.WriteLine("while (true)");
            using (var whilebrace = isb.Brace())
            {
                isb.WriteLine("if (reader.NodeType == XmlNodeType.Element)");
                using (var ifbrace = isb.Brace())
                {

                    isb.WriteLine("switch (reader.Name)");
                    using var switchbrace = isb.Brace(); // disposed at end of block
                    foreach (var param in model.Params.Where(p => p.Position != XmlPosition.Attribute))
                    {
                        isb.WriteLine($"case \"{param.XmlName}\":");
                        using (var casebrace = isb.Brace())
                        {
                            if (param.ImmutableArrayOf)
                            {
                                isb.WriteLine($"List<{param.FullyQualifiedType}> list = new();");
                                isb.WriteLine("while (reader.Read())");
                                using (var innerreadbrace = isb.Brace())
                                {
                                    isb.WriteLine($"if (reader.NodeType == XmlNodeType.Element && reader.Name == \"{param.XmlChildName}\")");
                                    using (var innerifbrace = isb.Brace())
                                    {
                                        if (param.HasPartialMethod)
                                            partialmethods.Add(param.GetParsePartialMethod());
                                        if (param.StringType)
                                        {
                                            isb.WriteLine($$"""
                                            string value = reader.ReadElementContentAsString();
                                            list.Add(value);
                                            """);
                                        }
                                        else if (param.TryParseable)
                                        {
                                            isb.WriteLine($$"""
                                            string value = reader.ReadElementContentAsString();
                                            if ({{param.GetTryParse("v", "value")}})
                                            {
                                                list.Add(v);
                                            }
                                            """);
                                        }
                                        else
                                        {
                                            isb.WriteLine($$"""
                                            {{param.GetValueAssignment("v", "reader", "default")}}
                                            list.Add(v);
                                            """);
                                        }
                                    }
                                    isb.WriteLine($$"""
                                    if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "{{param.XmlName}}")
                                        break;
                                    """);
                                }
                                isb.WriteLine($$"""
                                MethodInfo field = typeof({{model.ClassIdentifier}})
                                    .GetProperty(nameof({{param.ParameterName}}))!
                                    .GetSetMethod()!;
                                field.Invoke(returnObject, new object?[] { list.ToImmutableArray() });
                                """);
                            }
                            else
                            {
                                if (param.HasPartialMethod)
                                    partialmethods.Add(param.GetParsePartialMethod());
                                if (param.Position == XmlPosition.Presence)
                                {
                                    isb.WriteLine($$"""
                                    MethodInfo field = typeof({{model.ClassIdentifier}})
                                        .GetProperty(nameof({{param.ParameterName}}))!
                                        .GetSetMethod()!;
                                    field.Invoke(returnObject, new object?[] { true });
                                    """);
                                }
                                else if (param.StringType)
                                {
                                    isb.WriteLine($$"""
                                    string value = reader.ReadElementContentAsString();
                                    MethodInfo field = typeof({{model.ClassIdentifier}})
                                        .GetProperty(nameof({{param.ParameterName}}))!
                                        .GetSetMethod()!;
                                    field.Invoke(returnObject, new object?[] { value });
                                    """);

                                }
                                else if (param.TryParseable)
                                {
                                    isb.WriteLine($$"""
                                    string value = reader.ReadElementContentAsString();
                                    if ({{param.GetTryParse("v", "value")}})
                                    {
                                        MethodInfo field = typeof({{model.ClassIdentifier}})
                                            .GetProperty(nameof({{param.ParameterName}}))!
                                            .GetSetMethod()!;
                                        field.Invoke(returnObject, new object?[] { v });
                                    }
                                    """);
                                }
                                else
                                {
                                    isb.WriteLine($$"""
                                    {{param.GetValueAssignment("v", "reader", $"defaultValue{(model.IsStruct ? "" : "?")}.{param.ParameterName}")}}
                                    MethodInfo field = typeof({{model.ClassIdentifier}})
                                        .GetProperty(nameof({{param.ParameterName}}))!
                                        .GetSetMethod()!;
                                    field.Invoke(returnObject, new object?[] { v });
                                    """);
                                }
                            }
                        }
                        isb.WriteLine("break;");
                    }
                }
                isb.WriteLine($$"""
                else reader.Read();
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == elementName)
                    break;
                else if (reader.NodeType == XmlNodeType.None)
                    break;
                """);
            }
            isb.WriteLine("return returnObject;");
        }
    }

}
