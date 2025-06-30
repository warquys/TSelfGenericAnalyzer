using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.Editing;
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
                var paramName = config.paramName;
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitleTSelfNestedName,
                        createChangedSolution: c => ChangeNameToTSelfAsync(paramName, context.Document, typeArgumentSyntax, c),
                        equivalenceKey: equivalenceKeyByName),
                    diagnostic);
            }

            if (config.attributeEnable && config.attributeSymbol != null)
            {
                var attribute = config.attributeSymbol;
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: CodeFixResources.CodeFixTitleTSelfNestedAttribute,
                        createChangedSolution: c => ChangeAddAttributeTSelfAsync(attribute, context.Document, typeArgumentSyntax, c),
                        equivalenceKey: equivalenceKeyByAttribute),
                    diagnostic);
            }
        }

        private async Task<Solution> ChangeAddAttributeTSelfAsync(INamedTypeSymbol attributeSymbol, Document document, TypeParameterSyntax typeParameter, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            //var syntaxRef = attributeSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            //if (syntaxRef == null)
            //    return document.Project.Solution;
            //var attributeDeclarationNode = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            //var attribute = generator.Attribute(attributeDeclarationNode);

            
            // TODO: In case of an attribute in an other namesapce, the attribute will not be referenced
            var attributeName = Regex.Replace(attributeSymbol.Name, "(\\S+)Attribute$", "$1");
            var attribute = generator.Attribute(generator.IdentifierName(attributeName));
            var attributeSyntax = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attributeName));

            var newTypeParameter = generator.AddAttributes(typeParameter, attribute) as TypeParameterSyntax;
            newTypeParameter = newTypeParameter.WithAdditionalAnnotations();
            editor.ReplaceNode(typeParameter, newTypeParameter);
            return editor.GetChangedDocument().Project.Solution;
        }

        private async Task<Solution> ChangeNameToTSelfAsync(string name, Document document, TypeParameterSyntax typeParameter, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(typeParameter, cancellationToken);
            var originalSolution = document.Project.Solution;
            // RenameOptions SymbolRenameOptions  
            var option = new SymbolRenameOptions();
            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, typeSymbol, option, name, cancellationToken).ConfigureAwait(false);
            return newSolution;
        }
    }
}
