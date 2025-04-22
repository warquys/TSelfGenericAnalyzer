using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = TSelfGeneric.Test.CSharpCodeFixVerifier<
    TSelfGeneric.TSelfGenericAnalyzer,
    TSelfGeneric.TSelfGenericCodeFixProvider>;

namespace TSelfGeneric.Test;

[TestClass]
public class TSelfGenericParamUnitTests
{
    string editorConfig = $"""
    [*.cs]
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName} = TSelfAttribute
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
            }
            """;

        var fixTest = """
            namespace Self
            {
                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class Implementation : ISelfRequested<Implementation> { }
            
                class BadImplementation : ISelfRequested<BadImplementation> { }
            }
            """;

        var expected = VerifyCS.Diagnostic("TSelfGeneric")
            .WithSpan(7, 46, 7, 60)
            .WithArguments("BadImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig);
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
            }
            """;

        var fixTest = """
            namespace Self
            {
                interface ISelfRequested<TOther, TSelf> where TSelf : ISelfRequested<TOther, TSelf> { }

                class Implementation : ISelfRequested<int, Implementation> { }
            
                class BadImplementation : ISelfRequested<int, BadImplementation> { }
            }
            """;

        var expected = VerifyCS.Diagnostic("TSelfGeneric")
            .WithSpan(7, 51, 7, 65)
            .WithArguments("BadImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig);
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
                }
            }
            """;

        var expected = VerifyCS.Diagnostic("TSelfGeneric")
            .WithSpan(8, 50, 8, 64)
            .WithArguments("BadImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig);
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

        var expected = VerifyCS.Diagnostic("TSelfGeneric")
            .WithSpan(7, 45, 7, 59)
            .WithArguments("BadImplementation");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig);
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