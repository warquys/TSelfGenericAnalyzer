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

public class TSelfGenericConfigAttributeTests
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

                interface IImplementation : ISelfRequested<IImplementation> { }

                struct implementation :  ISelfRequested<implementation> { }
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

                interface IBadImplementation : ISelfRequested<{|#1:Implementation|}> { }

                struct implementation : ISelfRequested<{|#2:Implementation|}> { }
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

                interface IBadImplementation : ISelfRequested<IBadImplementation> { }

                struct implementation : ISelfRequested<implementation> { }
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
            .WithArguments("implementation");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0, expected1, expected2 }, fixTest, editorConfig);
    }

    [TestMethod]
    public async Task TestMultipleConfiguredNames()
    {
        var multipleNamesEditorConfig = $"""
        [*.cs]
        {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName} = First.TSelfAttribute; Second.SelfAttribute
        {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName}.enable = true
        {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName} = TSelf, TSelfType
        {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName}.enable = true
        """;

        var test = """
            using System;

            namespace First
            {
                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }
            }

            namespace Second
            {
                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class SelfAttribute : Attribute { }
            }

            interface IFirst<[First.TSelf] T> where T : IFirst<T> { }
            interface ISecond<[Second.Self] T> where T : ISecond<T> { }

            class FirstImplementation : IFirst<FirstImplementation> { }
            class SecondImplementation : ISecond<SecondImplementation> { }

            abstract class GenericImplementation<TSelfType> : IFirst<TSelfType>
                where TSelfType : GenericImplementation<TSelfType> { }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test, multipleNamesEditorConfig);
    }
}