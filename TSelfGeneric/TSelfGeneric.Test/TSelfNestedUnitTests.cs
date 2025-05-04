using Microsoft;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = TSelfGeneric.Test.CSharpCodeFixVerifier<
    TSelfGeneric.TSelfGenericAnalyzer,
    TSelfGeneric.TSelfNestedFixProvider>;
using Microsoft.CodeAnalysis.CSharp;
using System;

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

                interface ISelfRequestedAttribute<[TSelf] T> where T : ISelfRequested<T> { }    
                
                class SelfRequestedAttribute<[TSelf] T> where T : SelfRequested<T> { }
            
                class ImplementationAttribute<[TSelf] T> : ISelfRequested<T> where T : ISelfRequested<T> { }
            
                class ClassImplementationAttribute<[TSelf] T> : SelfRequested<T> where T : SelfRequested<T>  { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(test, editorConfig);
    }

    [TestMethod]
    public async Task InvalidValidNameCaseFixAttirubte()
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
            }
            """;

        var expected = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(0)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig, TSelfNestedFixProvider.equivalenceKeyByAttribute);
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
            }
            """;

        var expected = VerifyCS.Diagnostic(TSelfGenericAnalyzer.DiagnosticId_Nested)
             .WithLocation(0)
             .WithArguments("\"TSelf\"|\"[Self.TSelfAttribute] T\"");
        await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest, editorConfig, TSelfNestedFixProvider.equivalenceKeyByName);
    }

    [TestMethod]
    public async Task Bug() 
    {
        var test = """
        namespace Self
        {
            using System;

            [System.AttributeUsage(AttributeTargets.GenericParameter, Inherited = false, AllowMultiple = false)]
            sealed class TSelfAttribute : Attribute {}

            public class SelfRequest<[TSelf] TSelf>
            {

            }

            // TOOD: l'héritage du TSelf n'est pas bon
            // TODO: la culture utiliser reste anglais cependant cella devrait être francais 
            public class Imp<[TSelf] T> : SelfRequest<T>
            {

            }
        }
        """;
        await VerifyCS.VerifyAnalyzerAsync(test, editorConfig);
    }


}
