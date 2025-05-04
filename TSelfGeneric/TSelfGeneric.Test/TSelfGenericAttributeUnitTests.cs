using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = TSelfGeneric.Test.CSharpCodeFixVerifier<
    TSelfGeneric.TSelfGenericAnalyzer,
    TSelfGeneric.TSelfSelfFixProvider>;

namespace TSelfGeneric.Test;

[TestClass]
public class TSelfGenericAttributeUnitTests
{
    string editorConfig = $"""
    [*.cs]
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName} = Self.TSelfAttribute
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName}.enable = true
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName} = TSelf
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName}.enable = false
    """;

    [TestMethod]
    public async Task TestValidCase()
    {
        var test = """
            namespace Self
            {
                using System;
            
                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }

                interface ISelfRequested<[TSelf] T> where T : ISelfRequested<T> { }    
                
                class SelfRequested<[TSelf] T> where T : SelfRequested<T> { }

                class Implementation : ISelfRequested<Implementation> { }
            
                class ClassImplementation : SelfRequested<ClassImplementation> { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test, editorConfig);
    }

    [TestMethod]
    public async Task TestAttributeTSelfDetection()
    {
        var test = """
            namespace Self
            {
                using System;

                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }
            
                interface ISelfRequested<[TSelf] T> where T : ISelfRequested<T> { }

                class Implementation : ISelfRequested<Implementation> { }

                class BadImplementation : ISelfRequested<{|#0:Implementation|}> { }
            }
            """;

        var fixTest = """
            namespace Self
            {
                using System;

                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }

                interface ISelfRequested<[TSelf] T> where T : ISelfRequested<T> { }
            
                class Implementation : ISelfRequested<Implementation> { }

                class BadImplementation : ISelfRequested<BadImplementation> { }
            }
            """;

        var expected = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Self)
            .WithLocation(0)
            .WithArguments("BadImplementation");

        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig);
    }

}