using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static TSelfGeneric.TSelfGenericAnalyzer;

namespace TSelfGeneric
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TSelfGenericAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId_Self = "TSG1";
        public const string DiagnosticId_Nested = "TSG2";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title_Self = new LocalizableResourceString(nameof(Resources.Self_AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat_Self = new LocalizableResourceString(nameof(Resources.Self_AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description_Self = new LocalizableResourceString(nameof(Resources.Self_AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Title_Nested = new LocalizableResourceString(nameof(Resources.Nested_AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat_Nested = new LocalizableResourceString(nameof(Resources.Nested_AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description_Nested = new LocalizableResourceString(nameof(Resources.Nested_AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor Rule_Self = new DiagnosticDescriptor(DiagnosticId_Self, Title_Self, MessageFormat_Self, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description_Self);
        private static readonly DiagnosticDescriptor Rule_Nested = new DiagnosticDescriptor(DiagnosticId_Nested, Title_Nested, MessageFormat_Nested, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description_Nested);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule_Self, Rule_Nested); } }

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

                    if (!config.AsValidAttribute(typeParameter) && !config.IsValidName(typeParameter))
                        continue;

                    TypeSyntax typeArgumentSyntax = typeSyntax.TypeArgumentList.Arguments[i];

                    if (typeArgument.Kind != SymbolKind.TypeParameter)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(classSymbol, typeArgument))
                        {
                            var diagnostic = Diagnostic.Create(Rule_Self, typeArgumentSyntax.GetLocation(), classDeclaration.Identifier.Text);
                            context.ReportDiagnostic(diagnostic);
                            return;
                        }
                    }
                    else
                    {
                        if (!config.AsValidAttribute(typeArgument) && !config.IsValidName(typeArgument))
                        {
                            string genericParamAlternative = config switch
                            {
                                { attributeEnable: true, paramNameEnable: true } => $"\"{config.paramName}\"|\"[{config.attributeName}] {typeArgument.Name}\"",
                                { attributeEnable: true } => $"\"[{config.attributeName}] {typeArgument.Name}\"",
                                { paramNameEnable: true } => $"\"{config.paramName}\"",
                                _ => throw new NotSupportedException()
                            };

                            var locationInCurrentDocument = typeArgument.Locations.FirstOrDefault(loc => loc.SourceTree == context.Node.SyntaxTree);
                            
                            var diagnostic = Diagnostic.Create(Rule_Nested, locationInCurrentDocument, genericParamAlternative);
                            context.ReportDiagnostic(diagnostic);
                            return;
                        }
                    }
                }
            }
        }

        public struct Config
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
            // This struct is just a way to organise the code and share it with the fix
#pragma warning disable RS1008 // Évitez de stocker des données par compilation dans les champs d'un analyseur de diagnostic.
            public INamedTypeSymbol attributeSymbol;
#pragma warning restore RS1008 // Évitez de stocker des données par compilation dans les champs d'un analyseur de diagnostic.

            public static Config From(SyntaxNodeAnalysisContext context)
                => From(context.Options.AnalyzerConfigOptionsProvider, context.Node.SyntaxTree, context.Compilation);

            public static Config From(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree, Compilation compilation)
            {
                var config = new Config();
                var options = provider.GetOptions(syntaxTree);

                if (!options.TryGetValue($"{Root}.{ParamName}.enable", out var rowRequiredParamNameEnable)
                    || !bool.TryParse(rowRequiredParamNameEnable, out config.paramNameEnable))
                    config.paramNameEnable = DefaultTargetGenericArgEnable;

                if (!options.TryGetValue($"{Root}.{ParamName}", out config.paramName))
                    config.paramName = DefaultTargetGenericArg;

                if (!options.TryGetValue($"{Root}.{AttributeName}.enable", out var rowRequiredAttributeNameEnable)
                    || !bool.TryParse(rowRequiredAttributeNameEnable, out config.attributeEnable))
                    config.attributeEnable = DefaultTargetAttributeEnable;

                if (!options.TryGetValue($"{Root}.{AttributeName}", out config.attributeName))
                    config.attributeName = DefaultTargetAttributeName;

                config.attributeSymbol = compilation.GetTypeByMetadataName(config.attributeName);

                return config;
            }

            public bool IsValidName(ITypeSymbol typeArgument)
                => paramNameEnable
                /*&& isAbstract*/
                && typeArgument is ITypeParameterSymbol typeParameter
                && paramName.Equals(typeParameter.Name, StringComparison.OrdinalIgnoreCase);

            public bool AsValidAttribute(ITypeSymbol typeArgument)
            {
                var typeSymbol = this.attributeSymbol;
                return attributeEnable
                      && typeArgument != null
                      && typeArgument.GetAttributes()
                                     .Any(p => SymbolEqualityComparer.Default.Equals(p.AttributeClass, typeSymbol));
                    //&& typeArgument.GetAttributes().Any(p => p.AttributeClass.Name == attributeName);
            }
                
        }
    }
}
