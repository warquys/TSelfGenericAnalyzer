using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = TSelfGeneric.Test.CSharpCodeFixVerifier<
    TSelfGeneric.TSelfGenericAnalyzer,
    TSelfGeneric.TSelfSelfFixProvider>;

namespace TSelfGeneric.Test;

[TestClass]
public class TSelfGenericParamUnitTests
{
    string editorConfig = $"""
    [*.cs]
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName} = Self.TSelfAttribute
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName}.enable = false
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName} = TSelf
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName}.enable = true
    """;

    //No diagnostics expected to show up
    [TestMethod]
    public async Task TestValidCase()
    {
        var test = """
            namespace Self
            {
                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    
                
                class SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                class Implementation : ISelfRequested<Implementation> { }
            
                class ClassImplementation : SelfRequested<ClassImplementation> { }

                interface IImplementation : ISelfRequested<IImplementation> { }
                
                struct implementation : ISelfRequested<implementation> { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test, editorConfig);
    }

    //Diagnostic and CodeFix both triggered and checked for
    [TestMethod]
    public async Task TestInvalidInterfaceMonoParam()
    {
        var test =  """
            namespace Self
            {
                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class Implementation : ISelfRequested<Implementation> { }
            
                class BadImplementation : ISelfRequested<{|#0:Implementation|}> { }

                interface IBadImplementation : ISelfRequested<{|#1:Implementation|}> { }

                struct badImplementation : ISelfRequested<{|#2:Implementation|}> { }
            }
            """;

        var fixTest = """
            namespace Self
            {
                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class Implementation : ISelfRequested<Implementation> { }
            
                class BadImplementation : ISelfRequested<BadImplementation> { }

                interface IBadImplementation : ISelfRequested<IBadImplementation> { }

                struct badImplementation : ISelfRequested<badImplementation> { }
            }
            """;

        var expected0 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(0)
            .WithArguments("BadImplementation");
        var expected1 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(1)
            .WithArguments("IBadImplementation");
        var expected2 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(2)
            .WithArguments("badImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0, expected1, expected2 }, fixTest, editorConfig);
    }

    [TestMethod]
    public async Task TestInvalidInterfaceMultiParam()
    {
        var test =  """
            namespace Self
            {
                interface ISelfRequested<TOther, TSelf> where TSelf : ISelfRequested<TOther, TSelf> { }

                class Implementation : ISelfRequested<int, Implementation> { }
            
                class BadImplementation : ISelfRequested<int, {|#0:Implementation|}> { }

                interface IBadImplementation : ISelfRequested<int, {|#1:Implementation|}> { }
            
                struct badImplementation : ISelfRequested<int, {|#2:Implementation|}> { }
            }
            """;

        var fixTest = """
            namespace Self
            {
                interface ISelfRequested<TOther, TSelf> where TSelf : ISelfRequested<TOther, TSelf> { }

                class Implementation : ISelfRequested<int, Implementation> { }
            
                class BadImplementation : ISelfRequested<int, BadImplementation> { }

                interface IBadImplementation : ISelfRequested<int, IBadImplementation> { }

                struct badImplementation : ISelfRequested<int, badImplementation> { }
            }
            """;

        var expected0 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(0)
            .WithArguments("BadImplementation");
        var expected1 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(1)
            .WithArguments("IBadImplementation");
        var expected2 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(2)
            .WithArguments("badImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0, expected1, expected2 }, fixTest, editorConfig);
    }

    [TestMethod]
    public async Task TestInvalidInterfaceSubClass()
    {
        var test =  """
            namespace Self
            {
                interface ISelfRequested<TSelf, TOther> where TSelf : ISelfRequested<TSelf, TOther> { }

                class Implementation : ISelfRequested<Implementation, int> { }

                class Deep {
                    class BadImplementation : ISelfRequested<{|#0:Implementation|}, int> { }
                    interface IBadImplementation : ISelfRequested<{|#1:Implementation|}, int> { }
                    struct badImplementation : ISelfRequested<{|#2:Implementation|}, int> { }
                 }
            }
            """;

        var fixTest = """
            namespace Self
            {
                interface ISelfRequested<TSelf, TOther> where TSelf : ISelfRequested<TSelf, TOther> { }

                class Implementation : ISelfRequested<Implementation, int> { }

                class Deep {
                    class BadImplementation : ISelfRequested<BadImplementation, int> { }
                    interface IBadImplementation : ISelfRequested<IBadImplementation, int> { }
                    struct badImplementation : ISelfRequested<badImplementation, int> { }
                 }
            }
            """;

        var expected0 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(0)
            .WithArguments("BadImplementation");
        var expected1 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(1)
            .WithArguments("IBadImplementation");
        var expected2 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(2)
            .WithArguments("badImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0, expected1, expected2 }, fixTest, editorConfig);
    }

    [TestMethod]
    public async Task TestInvalidClass()
    {
        var test =  """
            namespace Self
            {
                class SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                class Implementation : SelfRequested<Implementation> { }
            
                class BadImplementation : SelfRequested<{|#0:Implementation|}> { }
            }
            """;

        var fixTest = """
            namespace Self
            {
                class SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                class Implementation : SelfRequested<Implementation> { }
            
                class BadImplementation : SelfRequested<BadImplementation> { }
            }
            """;

        var expected0 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(0)
            .WithArguments("BadImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0 }, fixTest, editorConfig);
    }

    //No diagnostics expected to show up
    [TestMethod]
    public async Task TestValidAbstractClass()
    {
        var test = """
            namespace Self
            {
                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                abstract class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : Implementation<TSelf> { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test, editorConfig);
    }
}