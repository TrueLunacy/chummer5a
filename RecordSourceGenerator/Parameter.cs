using System;
using System.Diagnostics.CodeAnalysis;

namespace RecordSourceGenerator
{
    public sealed record Parameter(string? ParameterName,
        string XmlName,
        string? XmlChildName,
        string? FullyQualifiedType,
        bool TypeIsValueType,
        string ContainingType,
        XmlPosition Position,
        ParamClass Class)
    {
        [MemberNotNullWhen(false, nameof(FullyQualifiedType))]
        [MemberNotNullWhen(false, nameof(ParameterName))]
        public bool TotalOverrideType => Class == ParamClass.TotalOverride;
        public bool StringType => FullyQualifiedType is "string" or "string?";
        private bool BoolType => FullyQualifiedType is "bool" or "bool?";
        public bool ImmutableArrayOf => XmlChildName is not null;

        // ParseableType is TryParse except if it's a string
        // Enum type is always TryParse
        // KnownXml, KnownConverter or Unknown is never TryParse
        public bool TryParseable
        {
            get
            {
                if (Class is ParamClass.ParsableType)
                {
                    return !StringType;
                }
                else
                    return Class is ParamClass.EnumType;
            }
        }

        // Returns the expression to do a TryParse
        public string GetTryParse(string param, string strvalue)
        {
            if (StringType) throw new InvalidOperationException();
            if (BoolType) return $"{FullyQualifiedType}.TryParse({strvalue}, out var {param})";
            return Class switch
            {
                ParamClass.ParsableType => $"{FullyQualifiedType}.TryParse({strvalue}, null, out var {param})",
                ParamClass.EnumType => $"Enum.TryParse<{FullyQualifiedType}>({strvalue}, out var {param})",
                _ => throw new InvalidOperationException()
            };
        }

        public bool HasPartialMethod => Class is ParamClass.UnknownType or ParamClass.TotalOverride;
        public string GetParsePartialMethod()
        {
            return Class switch
            {
                ParamClass.UnknownType => $"private static partial {FullyQualifiedType} Parse{ParameterName}(XmlReader reader, {FullyQualifiedType} defaultValue);",
                ParamClass.TotalOverride => $"private static partial void Parse{XmlName.Csharpify()}(XmlReader reader, {ContainingType} obj);",
                _ => throw new InvalidOperationException()
            };
        }

        public string GetWritePartialMethod()
        {
            return Class switch
            {
                ParamClass.UnknownType => $"private static partial void Write{ParameterName}({FullyQualifiedType} value, XmlWriter writer, string elementName);",
                ParamClass.TotalOverride => $"private partial void WriteUnknownValues(XmlWriter writer);",
                _ => throw new InvalidOperationException()
            };
        }

        public string GetTotalOverrideStatement(string reader, string obj)
        {
            if (Class != ParamClass.TotalOverride)
                throw new InvalidOperationException();
            return $"Parse{XmlName.Csharpify()}({reader}, {obj});";
        }

        public string GetValueAssignment(string param, string reader, string defaultvalue)
        {
            return Class switch
            {
                ParamClass.KnownXmlRecordType => $"{FullyQualifiedType} {param} = {FullyQualifiedType}.Read({reader}, {defaultvalue});",
                ParamClass.UnknownType => $"{FullyQualifiedType} {param} = Parse{ParameterName}({reader}, {defaultvalue});",
                ParamClass.KnownConverterType => throw new NotImplementedException(),
                _ => throw new InvalidOperationException()
            };
        }

        public string WriteStatement(string writer, string elementName, string? value = null)
        {
            value ??= ParameterName;
            return Class switch
            {
                ParamClass.KnownXmlRecordType => $"{value}.Write({writer}, \"{elementName}\");",
                ParamClass.UnknownType => $"Write{ParameterName}({value}, {writer}, \"{elementName}\");",
                ParamClass.KnownConverterType => throw new NotImplementedException(),
                _ => throw new InvalidOperationException()
            };
        }

        public static string WriteUnknownValuesCall(string writer) => $"WriteUnknownValues({writer});";
    }
}
