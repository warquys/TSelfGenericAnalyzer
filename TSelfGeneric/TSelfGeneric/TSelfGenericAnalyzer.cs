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
        public const string DiagnosticId_Self = "TSG001";
        public const string DiagnosticId_Nested = "TSG002";

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
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.InterfaceDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.StructDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.RecordStructDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.RecordDeclaration);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
            if (typeDeclaration.BaseList == null) return;

            var config = Config.From(context);
            if (!config.paramNameEnable && !config.attributeEnable) return;

            INamedTypeSymbol typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration);
            //bool isAbstract = typeSymbol.IsAbstract; // I do not see why the class should be abstract, it's better, maybe a case where it's normale?... maybe in the futur

            foreach (BaseTypeSyntax baseType in typeDeclaration.BaseList.Types)
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
                        if (SymbolEqualityComparer.Default.Equals(typeSymbol, typeArgument))
                            continue; // TSelf get passed the delcared class. Exacly what we whant

                        ReportSelfDiagnostic(context, typeArgumentSyntax, typeDeclaration.Identifier.Text);
                        return;
                    }
                    else
                    {
                        if (config.AsValidAttribute(typeArgument) || config.IsValidName(typeArgument))
                            continue; // TSelf is passed down using a valid sytaxe

                        ReportNestedDiagnostic(context, config, typeArgument, typeArgumentSyntax);
                        return;
                    }
                }
            }
        }

        private static void ReportSelfDiagnostic(SyntaxNodeAnalysisContext context, TypeSyntax typeArgumentSyntax, string className)
        {
            var diagnostic = Diagnostic.Create(Rule_Self, typeArgumentSyntax.GetLocation(), className);
            context.ReportDiagnostic(diagnostic);
        }

        private static void ReportNestedDiagnostic(SyntaxNodeAnalysisContext context, Config config, ITypeSymbol typeArgument, TypeSyntax typeArgumentSyntax)
        {
            string genericParamAlternative = config.GetGenericParameterAlternatives(typeArgument.Name);

            var locationInCurrentDocument = typeArgument.Locations.First(loc => loc.SourceTree == context.Node.SyntaxTree);

            var diagnostic = Diagnostic.Create(Rule_Nested, locationInCurrentDocument, genericParamAlternative);
            context.ReportDiagnostic(diagnostic);
        }

        public struct Config
        {
            public const string Root = "dotnet_tselfgeneric";
            public const string ParamName = "tself_param_name";
            public const string AttributeName = "tself_attribute_name";

            public const string DefaultTargetGenericArg = "TSelf";
            public const string DefaultTargetAttributeFullName = "TSelfAttribute";
            public const bool DefaultTargetGenericArgEnable = true;
            public const bool DefaultTargetAttributeEnable = true;

            public ImmutableArray<string> paramNames;
            public bool paramNameEnable;

            public ImmutableArray<string> attributeNames;
            public bool attributeEnable;
            // This struct is just a way to organise the code and share it with the fix
#pragma warning disable RS1008 // Évitez de stocker des données par compilation dans les champs d'un analyseur de diagnostic.
            public ImmutableArray<INamedTypeSymbol> attributeSymbols;
#pragma warning restore RS1008 // Évitez de stocker des données par compilation dans les champs d'un analyseur de diagnostic.

            public static Config Default(Compilation compilation)
            {
                var config = new Config();
                config.paramNameEnable = DefaultTargetGenericArgEnable;
                config.paramNames = ImmutableArray.Create(DefaultTargetGenericArg);
                config.attributeEnable = DefaultTargetAttributeEnable;
                config.attributeNames = ImmutableArray.Create(DefaultTargetAttributeFullName);
                config.attributeSymbols = GetAttributeSymbols(compilation, config.attributeNames);
                return config;
            }

            public static Config From(SyntaxNodeAnalysisContext context)
                => From(context.Options.AnalyzerConfigOptionsProvider, context.Node.SyntaxTree, context.Compilation);

            public static Config From(AnalyzerConfigOptionsProvider provider, SyntaxTree syntaxTree, Compilation compilation)
            {
                var config = new Config();
                var options = provider.GetOptions(syntaxTree);
                if (!options.TryGetValue($"{Root}.{ParamName}.enable", out var rowRequiredParamNameEnable)
                    || !bool.TryParse(rowRequiredParamNameEnable, out config.paramNameEnable))
                    config.paramNameEnable = DefaultTargetGenericArgEnable;

                config.paramNames = GetConfiguredValues(options, $"{Root}.{ParamName}", DefaultTargetGenericArg);

                if (!options.TryGetValue($"{Root}.{AttributeName}.enable", out var rowRequiredAttributeNameEnable)
                    || !bool.TryParse(rowRequiredAttributeNameEnable, out config.attributeEnable))
                    config.attributeEnable = DefaultTargetAttributeEnable;

                config.attributeNames = GetConfiguredValues(options, $"{Root}.{AttributeName}", DefaultTargetAttributeFullName);
                config.attributeSymbols = GetAttributeSymbols(compilation, config.attributeNames);

                return config;
            }

            private static ImmutableArray<string> GetConfiguredValues(AnalyzerConfigOptions options, string key, string defaultValue)
            {
                if (!options.TryGetValue(key, out var value))
                    value = defaultValue;

                return value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())
                    .Where(item => item.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToImmutableArray();
            }

            private static ImmutableArray<INamedTypeSymbol> GetAttributeSymbols(Compilation compilation, ImmutableArray<string> names)
                => names.Select(compilation.GetTypeByMetadataName)
                    .Where(symbol => symbol != null)
                    .ToImmutableArray();

            public bool IsValidName(ITypeSymbol typeArgument)
                => paramNameEnable
                /*&& isAbstract*/
                && typeArgument is ITypeParameterSymbol typeParameter
                && paramNames.Any(name => name.Equals(typeParameter.Name, StringComparison.OrdinalIgnoreCase));

            public bool AsValidAttribute(ITypeSymbol typeArgument)
            {
                var configuredAttributeSymbols = attributeSymbols;
                return attributeEnable
                      && typeArgument != null
                      && typeArgument.GetAttributes()
                                     .Any(attribute => configuredAttributeSymbols.Any(attributeSymbol =>
                                         SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol)));
            }

            public string GetGenericParameterAlternatives(string typeParameterName)
            {
                var alternatives = new System.Collections.Generic.List<string>();
                if (paramNameEnable)
                    alternatives.AddRange(paramNames.Select(name => $"\"{name}\""));
                if (attributeEnable)
                    alternatives.AddRange(attributeNames.Select(name => $"\"[{GetAttributeUsageName(name)}] {typeParameterName}\""));

                return string.Join("|", alternatives);
            }

            private static string GetAttributeUsageName(string metadataName)
            {
                var lastDot = metadataName.LastIndexOf('.');
                var simpleName = lastDot >= 0 ? metadataName.Substring(lastDot + 1) : metadataName;
                return simpleName.EndsWith("Attribute", StringComparison.Ordinal)
                    ? simpleName.Substring(0, simpleName.Length - "Attribute".Length)
                    : simpleName;
            }
        }
    }
}
