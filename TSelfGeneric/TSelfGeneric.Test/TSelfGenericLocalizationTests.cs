using System.Globalization;
using System.Resources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSelfGeneric.Test;

[TestClass]
public class TSelfGenericLocalizationTests
{
    [TestMethod]
    public void FrenchAnalyzerResourcesAreUsed()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var french = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = french;
            CultureInfo.CurrentUICulture = french;

            var manager = new ResourceManager("TSelfGeneric.Resources", typeof(TSelfGenericAnalyzer).Assembly);

            Assert.AreEqual(
                "Le paramètre générique devrait être dans la continuité du TSelf.",
                manager.GetString("Nested_AnalyzerDescription", french));
            Assert.AreEqual(
                "Le paramètre générique TSelf n'est pas la classe implémentant/héritant",
                manager.GetString("Self_AnalyzerTitle", french));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [TestMethod]
    public void FrenchCodeFixResourcesAreUsed()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var french = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentCulture = french;
            CultureInfo.CurrentUICulture = french;

            var manager = new ResourceManager(
                "TSelfGeneric.CodeFixResources",
                typeof(TSelfNestedFixProvider).Assembly);

            Assert.AreEqual(
                "Utiliser la classe implémentant/héritant l'interface",
                manager.GetString("CodeFixTSelfSelf", french));
            Assert.AreEqual(
                "Ajouter l'attribut TSelf au paramètre générique",
                manager.GetString("CodeFixTitleTSelfNestedAttribute", french));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    [DataTestMethod]
    [DataRow("es")]
    [DataRow("de")]
    [DataRow("it")]
    [DataRow("pt-BR")]
    public void AdditionalLanguageResourcesAreAvailable(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var analyzerManager = new ResourceManager("TSelfGeneric.Resources", typeof(TSelfGenericAnalyzer).Assembly);
        var codeFixManager = new ResourceManager(
            "TSelfGeneric.CodeFixResources",
            typeof(TSelfNestedFixProvider).Assembly);

        Assert.IsFalse(string.IsNullOrWhiteSpace(analyzerManager.GetString("Self_AnalyzerTitle", culture)));
        Assert.IsFalse(string.IsNullOrWhiteSpace(analyzerManager.GetString("Nested_AnalyzerMessageFormat", culture)));
        Assert.IsFalse(string.IsNullOrWhiteSpace(codeFixManager.GetString("CodeFixTSelfSelf", culture)));
        Assert.IsFalse(string.IsNullOrWhiteSpace(codeFixManager.GetString("CodeFixTitleTSelfNestedName", culture)));
    }
}
