using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace RecordSourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SourceAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            Diagnostics.MustBeRecordType,
            Diagnostics.CannotBeDerived,
            Diagnostics.MustHaveConstructorAttribute,
            Diagnostics.MustBePartialType,
            Diagnostics.MustBeSealedType,
            Diagnostics.CannotHaveBothElementAndAttr,
            Diagnostics.MustBeSimpleType,
        ];

        public override void Initialize(AnalysisContext context)
        {
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            if (!Debugger.IsAttached) context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        // todo: enforce XmlAsPresence to be boolean
        // todo: ensure no duplicate names
        // todo: ensure no child is named the same as the parent?
        private static void AnalyzeSymbol(SymbolAnalysisContext ctx)
        {
            INamedTypeSymbol type = (INamedTypeSymbol)ctx.Symbol;

            // require attr.
            if (!type.GetAttributes().Any(a => a.AttributeClass?.Name == SourceGenerator.RecordAttr))
                return;

            bool reported = RecordTypeDiagnostics(type, in ctx);
            if (reported) return;

            var model = TypeParser.FromType(type);
            Debug.Assert(model is not null);

            _ = MethodDiagnostics(type, model!, in ctx);
            _ = AttributeElementDiagnostics(type, model!, in ctx);
        }

        private static bool AttributeElementDiagnostics(INamedTypeSymbol type, Model model, in SymbolAnalysisContext ctx)
        {
            bool reported = false;

            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                var modelprop = model.Params.Single(p => p.ParameterName == property.Name);

                var attra = property.GetAttributes().SingleOrDefault(a => a.AttributeClass?.Name == SourceGenerator.AttributeAttr);
                var elementa = property.GetAttributes().SingleOrDefault(a => a.AttributeClass?.Name == SourceGenerator.ElementAttr);

                if (attra is not null && elementa is not null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.CannotHaveBothElementAndAttr,
                        property.Locations[0],
                        property.Name
                    ));
                    reported = true;
                }

                if ((attra is not null && modelprop.ImmutableArrayOf)
                    || modelprop.Class is not ParamClass.ParsableType or ParamClass.EnumType)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.MustBeSimpleType,
                        property.Locations[0],
                        property.Name
                    ));
                    reported = true;
                }
            }

            return reported;
        }

        private static bool MethodDiagnostics(INamedTypeSymbol type, Model model, in SymbolAnalysisContext ctx)
        {
            bool reported = false;

            // if we have multiple constructors (other than the copy constructor) we must disambiguate with the XmlConstructorAttribute
            // Having only one constructor is illegal, the primary constructor can't conflict with the synthesized copy constructor (CS8910)
            if (type.Constructors.Length != 2)
            {
                if (!type.Constructors.Any(c => c.GetAttributes().Any(a => a.AttributeClass?.Name == SourceGenerator.ConstructorAttr)))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Diagnostics.MustHaveConstructorAttribute,
                        type.Locations[0],
                        type.Name
                    ));
                    reported = true;
                }
            }

            // if any methods require a partial method, check for them and issue a diagnostic
            // so we can have a code fix for the issues and report errors at the right place
#warning todo: this

            return reported;
        }

        private static bool RecordTypeDiagnostics(INamedTypeSymbol type, in SymbolAnalysisContext ctx)
        {
            bool reported = false;

            if (!type.IsRecord)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MustBeRecordType,
                    type.Locations[0],
                    type.Name
                ));
                reported = true;
            }

            if (!type.IsPartial())
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MustBePartialType,
                    type.Locations[0],
                    type.Name
                ));
                reported = true;
            }

            if (type.BaseType?.Name is not nameof(Object) or nameof(ValueType))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.CannotBeDerived,
                    type.Locations[0],
                    type.Name
                ));
                reported = true;
            }

            if (!type.IsSealed && !type.IsValueType)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MustBeSealedType,
                    type.Locations[0],
                    type.Name
                ));
                reported = true;
            }

            return reported;
        }
    }

}
