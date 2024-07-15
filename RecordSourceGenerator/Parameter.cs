using System;

namespace RecordSourceGenerator
{
    public sealed record Parameter(string ParameterName,
        string XmlName,
        string? XmlChildName,
        string FullyQualifiedType,
        bool TypeIsValueType,
        XmlPosition Position,
        ParamClass Class)
    {
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

        public bool HasPartialMethod => Class is ParamClass.UnknownType;
        public string GetParsePartialMethod()
        {
            if (Class is not ParamClass.UnknownType)
                throw new InvalidOperationException();
            return $"private static partial {FullyQualifiedType} Parse{ParameterName}(XmlReader reader, {FullyQualifiedType} defaultValue);";
        }

        public string GetWritePartialMethod()
        {
            if (Class is not ParamClass.UnknownType)
                throw new InvalidOperationException();
            return $"private static partial void Write{ParameterName}({FullyQualifiedType} value, XmlWriter writer, string elementName);";
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
    }
}
