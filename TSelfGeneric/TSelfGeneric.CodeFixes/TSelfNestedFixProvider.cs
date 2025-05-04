using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;

namespace TSelfGeneric
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TSelfNestedFixProvider)), Shared]
    public sealed class TSelfNestedFixProvider : CodeFixProvider
    {
        public const string equivalenceKeyByName = nameof(TSelfNestedFixProvider) + "Name";
        public const string equivalenceKeyByAttribute = nameof(TSelfNestedFixProvider) + "Attribute";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(TSelfGenericAnalyzer.DiagnosticId_Nested); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            
            // Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            
            // Find the type argument identified by the diagnostic.
            var typeArgumentSyntax = root.FindNode(diagnosticSpan) as TypeParameterSyntax;
            if (typeArgumentSyntax == null) return;

            var semanticModel = await context.Document.GetSemanticModelAsync().ConfigureAwait(false);
            var tree = await context.Document.GetSyntaxTreeAsync().ConfigureAwait(false);
            var configProvider = context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            var config = TSelfGenericAnalyzer.Config.From(configProvider, tree, semanticModel.Compilation);

            // Register a code action that will invoke the fix.
            if (config.paramNameEnable && config.paramName != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitleTSelfNestedName,
                        createChangedSolution: c => ChangeNameToTSelfAsync(config, context.Document, typeArgumentSyntax, c),
                        equivalenceKey: equivalenceKeyByName),
                    diagnostic);
            }

            if (config.attributeEnable && config.attributeSymbol != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitleTSelfNestedAttribute,
                        createChangedSolution: c => ChangeAddAttributeTSelfAsync(config, context.Document, typeArgumentSyntax, c),
                        equivalenceKey: equivalenceKeyByAttribute),
                    diagnostic);
            }
        }

        private async Task<Solution> ChangeAddAttributeTSelfAsync(TSelfGenericAnalyzer.Config config, Document document, TypeParameterSyntax typeParameter, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Take the namespace before editing the document.
            var @namespace = await GetNamespaceAsync(typeParameter, document, cancellationToken);
            var attributeName = Regex.Replace(config.attributeSymbol.Name, "(\\S+)Attribute$", "$1");
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));
            // TODO: check the consequence if the attribute is an attribute that do not existe
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
            var newTypeParameter = typeParameter.AddAttributeLists(attributeList);

            var newRoot = root.ReplaceNode(typeParameter, newTypeParameter);
            var newDocument = document.WithSyntaxRoot(newRoot);
            newDocument = await EnsureAttributeAccessibilityAsync(config.attributeSymbol, @namespace, newDocument, cancellationToken).ConfigureAwait(false);

            return newDocument.Project.Solution;
        }

        private async Task<Document> EnsureAttributeAccessibilityAsync(
            INamedTypeSymbol attribute,
            INamespaceSymbol @namespace,
            Document document,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (SymbolEqualityComparer.Default.Equals(@namespace, attribute.ContainingNamespace))
                return document;

            if (root is not CompilationUnitSyntax compilationUnit)
                return document;
            
            var namespaceName = attribute.ContainingNamespace.ToDisplayString();
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            if (usings.Any(u => u.Name.ToString() == namespaceName))
                return document;

            var newUsing = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName));
            var newCompilationUnit = compilationUnit.AddUsings(newUsing);
            var newDocument = document.WithSyntaxRoot(newCompilationUnit);
            return newDocument;
        }

        private async Task<INamespaceSymbol> GetNamespaceAsync(TypeParameterSyntax targetTypeParameter, Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (targetTypeParameter.SyntaxTree != semanticModel.SyntaxTree)
                throw new InvalidOperationException("targetTypeParameter does not belong to the same SyntaxTree as the SemanticModel.");

            var symbol = semanticModel.GetDeclaredSymbol(targetTypeParameter, cancellationToken);
            if (symbol == null)
                return null;
            
            if (symbol.ContainingNamespace != null)
                return symbol.ContainingNamespace;
            
            if (symbol is ITypeParameterSymbol typeParameterSymbol)
                return typeParameterSymbol.ContainingSymbol.ContainingNamespace;

            return null;
        }

        private async Task<Solution> ChangeNameToTSelfAsync(TSelfGenericAnalyzer.Config config, Document document, TypeParameterSyntax typeParameter, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeParameter, cancellationToken);
            var originalSolution = document.Project.Solution;
            var optionSet = originalSolution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, config.paramName, optionSet, cancellationToken).ConfigureAwait(false);
            return newSolution;
        }
    }
}
