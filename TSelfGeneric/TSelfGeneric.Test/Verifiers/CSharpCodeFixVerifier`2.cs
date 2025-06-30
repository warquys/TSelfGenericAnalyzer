using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace TSelfGeneric.Test
{
    public static partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic();

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        // TODO: Doc
        public static async Task VerifyAnalyzerAsync(string source, string config, params DiagnosticResult[] expected)
        {
            var test = new Test
            {
                TestCode = source,
                TestState =
                {
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", config)
                    }
                },
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, string)"/>
        public static async Task VerifyCodeFixAsync(string source, string fixedSource)
            => await VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult, string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        /// <inheritdoc cref="CodeFixVerifier{TAnalyzer, TCodeFix, TTest, TVerifier}.VerifyCodeFixAsync(string, DiagnosticResult[], string)"/>
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
            var test = new Test
            {
                TestCode = source,
                FixedCode = fixedSource,
            };

            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync(CancellationToken.None);
        }

        // TODO: Doc
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, string config, string codeAction = null)
            => await VerifyCodeFixAsync(source, new[] { expected }, fixedSource, config, codeAction);

        // TODO: Doc
        public static async Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, string config, string codeAction = null)
        {
            var test =  new Test()
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionEquivalenceKey = codeAction,
                TestState =
                {
                    AnalyzerConfigFiles =
                    {
                        ("/.editorconfig", config)
                    }
                },
                //TestState =
                //{
                //    AdditionalFiles = { (".editorconfig", editorConfig) }
                //},
                // ExpectedDiagnostics = { expected },
            };
            test.ExpectedDiagnostics.AddRange(expected);

            //runner.SolutionTransforms.Add((solution, projectId) =>
            //{
            //    var documentId = DocumentId.CreateNewId(projectId, ".editorconfig");
            //    return solution.AddAnalyzerConfigDocument(documentId, ".editorconfig", SourceText.From(editorConfig), filePath: "/.editorconfig");
            //});
            await test.RunAsync(CancellationToken.None);
        }

    }
}
