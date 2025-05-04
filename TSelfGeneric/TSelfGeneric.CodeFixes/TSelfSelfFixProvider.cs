using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TSelfGeneric
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TSelfSelfFixProvider)), Shared]
    public sealed class TSelfSelfFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(TSelfGenericAnalyzer.DiagnosticId_Self); }
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
            var typeArgumentSyntax = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<TypeSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTSelfSelf,
                    createChangedSolution: c => ChangeToSelfAsync(context.Document, typeArgumentSyntax, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTSelfSelf)),
                diagnostic);
        }

        private async Task<Solution> ChangeToSelfAsync(Document document, TypeSyntax typeArgumentSyntax, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Find the containing class declaration
            var classDeclaration = typeArgumentSyntax.Ancestors().OfType<TypeDeclarationSyntax>().First();
            var className = classDeclaration.Identifier.Text;

            // Create a new type argument with the class name
            var newTypeArgumentSyntax = SyntaxFactory.ParseTypeName(className).WithTriviaFrom(typeArgumentSyntax);

            // Replace the old type argument with the new one
            var newRoot = root.ReplaceNode(typeArgumentSyntax, newTypeArgumentSyntax);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument.Project.Solution;
        }
    }
}
