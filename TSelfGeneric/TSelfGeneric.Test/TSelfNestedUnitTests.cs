using Microsoft;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = TSelfGeneric.Test.CSharpCodeFixVerifier<
    TSelfGeneric.TSelfGenericAnalyzer,
    TSelfGeneric.TSelfNestedFixProvider>;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace TSelfGeneric.Test;

[TestClass]
public class TSelfNestedUnitTests
{
    string editorConfig = $"""
    [*.cs]
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName} = Self.TSelfAttribute
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.AttributeName}.enable = true
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName} = TSelf
    {TSelfGenericAnalyzer.Config.Root}.{TSelfGenericAnalyzer.Config.ParamName}.enable = true
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

                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    
                
                class SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf>{ }
            
                class ClassImplementation<TSelf> : SelfRequested<TSelf> where TSelf : SelfRequested<TSelf> { }

                interface IImplementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf>{ }

                struct implementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf>{ }

                interface ISelfRequestedAttribute<[TSelf] T> where T : ISelfRequested<T> { }    
                
                class SelfRequestedAttribute<[TSelf] T> where T : SelfRequested<T> { }
            
                class ImplementationAttribute<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T> { }
            
                class ClassImplementationAttribute<[TSelf] T> : SelfRequested<T> where T : SelfRequested<T>  { }

                interface IImplementationAttribute<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T>  { }

                struct implementationAttribute<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T>{ }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test, editorConfig);
    }

    [TestMethod]
    public async Task InvalidValidNameCaseFixAttributed()
    {
        var test = """
            namespace Self
            {
                using System;

                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }

                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    

                class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class BadImplementation<{|#0:T|}> : ISelfRequested<T> where T : ISelfRequested<T> { }

                interface IBadImplementation<{|#1:T|}> : ISelfRequested<T> where T : ISelfRequested<T> { }

                struct badImplementation<{|#2:T|}> : ISelfRequested<T> where T : ISelfRequested<T>{ }
            }
            """;

        var fixTest = """
            namespace Self
            {
                using System;

                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }

                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    

                class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class BadImplementation<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T> { }

                interface IBadImplementation<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T> { }

                struct badImplementation<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T>{ }
            }
            """;

        var expected0 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(0)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        var expected1 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(1)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        var expected2 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(2)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0, expected1, expected2 }, fixTest, editorConfig, TSelfNestedFixProvider.equivalenceKeyByAttribute);
    }

    [TestMethod]
    public async Task InvalidValidNameCaseFixName()
    {
        var test = """
            namespace Self
            {
                using System;

                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }

                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    

                class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class BadImplementation<{|#0:T|}> : ISelfRequested<T> where T : ISelfRequested<T> { }

                interface IBadImplementation<{|#1:T|}> : ISelfRequested<T> where T : ISelfRequested<T> { }

                struct badImplementation<{|#2:T|}> : ISelfRequested<T> where T : ISelfRequested<T> { }
            }
            """;

        var fixTest = """
            namespace Self
            {
                using System;

                [AttributeUsage(AttributeTargets.GenericParameter)]
                public class TSelfAttribute : Attribute { }

                interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }    

                class Implementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                class BadImplementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                interface IBadImplementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

                struct badImplementation<TSelf> : ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }
            }
            """;

        var expected0 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(0)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        var expected1 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(1)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        var expected2 = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(2)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        await VerifyCS.VerifyCodeFixAsync(test, new DiagnosticResult[] { expected0, expected1, expected2 }, fixTest, editorConfig, TSelfNestedFixProvider.equivalenceKeyByName);
    }
}
