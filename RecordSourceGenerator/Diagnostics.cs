using Microsoft.CodeAnalysis;

namespace RecordSourceGenerator
{
    internal static class Diagnostics
    {
        public static DiagnosticDescriptor MustBeRecordType = new DiagnosticDescriptor(
            id: "XRS0001",
            title: $"{SourceGenerator.RecordAttr} must be used on record types only",
            messageFormat: $"Type {{0}} is not a record type. Either remove {SourceGenerator.RecordAttr} or change it to be a record type.",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{SourceGenerator.RecordAttr} must be used on record types only."
        );

        public static DiagnosticDescriptor CannotBeDerived = new DiagnosticDescriptor(
            id: "XRS0002",
            title: $"{SourceGenerator.RecordAttr}-marked types should not be derived types",
            messageFormat: $"Type {{0}} is derived from another record type. Either remove {SourceGenerator.RecordAttr} or change it to be a record type.",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{SourceGenerator.RecordAttr}-marked types should not be derived types."
        );

        public static DiagnosticDescriptor MustHaveConstructorAttribute = new DiagnosticDescriptor(
            id: "XRS0003",
            title: $"{SourceGenerator.RecordAttr}-marked types should have a constructor with {SourceGenerator.ConstructorAttr}",
            messageFormat: $"Type {{0}} has multiple constructors, but none are decorated with {SourceGenerator.ConstructorAttr}. Add the attribute to the correct constructor.",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{SourceGenerator.RecordAttr}-marked types should have a constructor with {SourceGenerator.ConstructorAttr}."
        );

        public static DiagnosticDescriptor MustBePartialType = new DiagnosticDescriptor(
            id: "XRS0004",
            title: $"{SourceGenerator.RecordAttr} must be used on partial types only",
            messageFormat: $"Type {{0}} is not a partial type. Either remove {SourceGenerator.RecordAttr} or change it to be a partial type.",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{SourceGenerator.RecordAttr} must be used on partial types only."
        );

        public static DiagnosticDescriptor MustBeSealedType = new DiagnosticDescriptor(
            id: "XRS0005",
            title: $"{SourceGenerator.RecordAttr} must be used on sealed types only",
            messageFormat: $"Type {{0}} is not a sealed type. Either remove {SourceGenerator.RecordAttr} or change it to be a sealed type.",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{SourceGenerator.RecordAttr} must be used on sealed types only."
        );

        public static DiagnosticDescriptor CannotHaveBothElementAndAttr = new DiagnosticDescriptor(
            id: "XRS0006",
            title: $"Cannot have both {SourceGenerator.AttributeAttr} and {SourceGenerator.ElementAttr} on the same property",
            messageFormat: $"Property {{0}} has both {SourceGenerator.AttributeAttr} and {SourceGenerator.ElementAttr}. Remove one of them.",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"Cannot have both {SourceGenerator.AttributeAttr} and {SourceGenerator.ElementAttr} on the same property."
        );

        public static DiagnosticDescriptor MustBeSimpleType = new DiagnosticDescriptor(
            id: "XRS0007",
            title: $"{SourceGenerator.AttributeAttr} types must be simple",
            messageFormat: $"Property {{0}} is {SourceGenerator.AttributeAttr} type, but is a complex type. Remove the attribute or change it to be a simple type (int, string, enums, IParseable<T>/ISpanParseable<T> types).",
            category: "XmlRecordSourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: $"{SourceGenerator.AttributeAttr} types must be simple."
        );

    }

}
