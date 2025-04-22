using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TSelfGeneric
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TSelfGenericAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "TSelfGeneric";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context) 
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (classDeclaration.BaseList == null) return;

            var config = Config.From(context);
            if (!config.paramNameEnable && !config.attributeEnable) return;

            INamedTypeSymbol classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);
            //bool isAbstract = classSymbol.IsAbstract; // I do not see why the class should be abstract, it's better, maybe a case where it's normale?... maybe in the futur

            foreach (BaseTypeSyntax baseType in classDeclaration.BaseList.Types)
            {
                if (baseType.Type is not GenericNameSyntax typeSyntax)
                    continue;

                SymbolInfo baseSymbolInfo = context.SemanticModel.GetSymbolInfo(typeSyntax);
                if (baseSymbolInfo.Symbol is not INamedTypeSymbol baseNamedTypeSymbol)
                    continue;

                int minLength = Math.Min(baseNamedTypeSymbol.TypeParameters.Length, baseNamedTypeSymbol.TypeArguments.Length);
                Debug.Assert(typeSyntax.TypeArgumentList.Arguments.Count >= minLength);
                for (int i = 0; i < minLength; i++)
                {
                    ITypeParameterSymbol typeParameter = baseNamedTypeSymbol.TypeParameters[i];
                    ITypeSymbol typeArgument = baseNamedTypeSymbol.TypeArguments[i];

                    if ((AsValidAttribute(typeParameter) || IsValidName(baseNamedTypeSymbol, typeParameter))
                        && typeArgument.Kind != SymbolKind.TypeParameter
                        && !SymbolEqualityComparer.Default.Equals(classSymbol, typeArgument))
                    {
                        TypeSyntax typeArgumentSyntax = typeSyntax.TypeArgumentList.Arguments[i];
                        var diagnostic = Diagnostic.Create(Rule, typeArgumentSyntax.GetLocation(), classDeclaration.Identifier.Text);
                        context.ReportDiagnostic(diagnostic);
                        return;
                    }
                }
            }

            bool IsValidName(INamedTypeSymbol baseNamedTypeSymbol, ITypeSymbol typeArgument)
                => config.paramNameEnable &&
                    /*isAbstract &&*/ typeArgument is ITypeParameterSymbol typeParam
                    && config.paramName.Equals(typeParam.Name, StringComparison.OrdinalIgnoreCase);

            bool AsValidAttribute(ITypeSymbol typeArgument)
                => config.attributeEnable &&
                   typeArgument.GetAttributes().Any(p => p.AttributeClass.Name == config.attributeName);
        }

        internal struct Config
        {
            public const string Root = "dotnet_tselfgeneric";
            public const string ParamName = "tself_param_name";
            public const string AttributeName = "tself_attribute_name";

            public const string DefaultTargetGenericArg = "TSelf";
            public const string DefaultTargetAttributeName = "TSelfAttribute";
            public const bool DefaultTargetGenericArgEnable = true;
            public const bool DefaultTargetAttributeEnable = false;

            public string paramName;
            public bool paramNameEnable;

            public string attributeName;
            public bool attributeEnable;

            public static Config From(SyntaxNodeAnalysisContext context)
            {
                var config = new Config();
                var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
                if (!options.TryGetValue($"{Root}.{ParamName}", out config.paramName))
                    config.paramName = DefaultTargetGenericArg;

                if (!options.TryGetValue($"{Root}.{ParamName}.enable", out var rowRequiredParamNameEnable)
                    || !bool.TryParse(rowRequiredParamNameEnable, out config.paramNameEnable))
                    config.paramNameEnable = DefaultTargetGenericArgEnable;

                if (!options.TryGetValue($"{Root}.{AttributeName}", out config.attributeName))
                    config.attributeName = DefaultTargetAttributeName;

                if (!options.TryGetValue($"{Root}.{AttributeName}.enable", out var rowRequiredAttributeNameEnable)
                    || !bool.TryParse(rowRequiredAttributeNameEnable, out config.attributeEnable))
                    config.attributeEnable = DefaultTargetAttributeEnable;

                return config;
            }
        }

    }
}
