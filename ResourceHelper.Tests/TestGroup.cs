using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Web;
using System.Web.Mvc;
using ResourceHelper;
using System.Web.Routing;
using System.IO;
using System.Configuration;

// Links: http://www.hanselman.com/blog/NuGetPackageOfTheWeek1ASPNETSpriteAndImageOptimization.aspx
// TODO: Support Html.ResourceGroup("~/Content/widget-small") // This index all the resource in widget-small and create sprite out of the images in the folder
//               Html.Resource("~/Content/widget-small/news.gif") // This creates an image tag pointing to the sprite, use Cache to store lookup map
//               <img class="test" style="@Html.Resource("~/Content/widget-small/news.gif")" />

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestGroup
    {
        string WebRoot;
       
        [SetUp]
        public void Init()
        {
            // Set sample as root
            WebRoot = FakeUtils.GetWebRoot(TestContext.CurrentContext.TestDirectory);
            Directory.SetCurrentDirectory(WebRoot);      
        }

        [Test]
        public void TestGroupAddOverlapping()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            // Add two groups
            html.ResourceGroup("jqueryui", "~/Content/themes/base/*.css"); // jquery.ui bundle
            html.ResourceGroup("all", "~/Content/*.css", true); // jquery.ui bundle

            // Make sure the groups get rendered by using at least one of the resources
            html.Resource("~/Content/Test.css");
            html.Resource("~/Content/themes/base/jquery.ui.dialog.css");

            // Render JQueryUI resources
            var htmlstr = html.RenderResources("jqueryui").ToHtmlString();
            StringAssert.Contains("jquery.ui.all.css", htmlstr);
            StringAssert.DoesNotContain("Site.css", htmlstr);
            StringAssert.DoesNotContain("Test.css", htmlstr);

            // Render JQueryUI resources
            htmlstr = html.RenderResources("all").ToHtmlString();
            StringAssert.Contains("Test.css", htmlstr);
            StringAssert.Contains("Site.css", htmlstr);
            StringAssert.DoesNotContain("jquery.ui.all.css", htmlstr);           
        }

        // TODO: Support Html.ResourceGroup("jquery", "~/Content/themes/base", @"^jquery\.ui.*\.css$") // This index all the resources in base that maches the regex and creates a bundle out of them
        //               Html.ResourceGroup("jquery", "~/Content/themes/base", @"^jquery\.ui.*\.css$", true) // Recurse into folder
        //               Html.Resource("~/Content/themes/base/jquery.ui.all.css") // Will make sure jquery.ui.all.css is first in the bundle
        [Test]
        public void TestGroupCSSBundle()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.ResourceSettings(new HTMLResourceOptions() { Bundle = true });

            // Add a group for jquery ui css files
            html.ResourceGroup("jqueryui", "~/Content/themes/base/*.css"); // jquery.ui bundle

            // Render one resource from the group and one from outside
            html.Resource("~/Content/themes/base/jquery.ui.core.css");
            html.Resource("~/Content/Site.css");

            // Render resourcs and verify that we get two bundles
            var htmlstr = html.RenderResources().ToHtmlString();
            StringAssert.Contains("jqueryui-bundle", htmlstr);
            
            StringAssert.Contains("all-bundle", htmlstr);
        }

        [Test]
        public void TestGroupCSSBundleExplicitRendering()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.ResourceSettings(new HTMLResourceOptions() { Bundle = true });
            
            // Add a group for jquery ui css files
            html.ResourceGroup("jqueryui", "~/Content/themes/base/*.css"); // jquery.ui bundle
            
            // Render one resource from the group and one from outside
            html.Resource("~/Content/themes/base/jquery.ui.core.css");
            html.Resource("~/Content/Site.css");
            
            // Render resources for jquery and verify that we only bundle
            var htmlstr = html.RenderResources("jqueryui").ToHtmlString();
            StringAssert.Contains("jqueryui-bundle", htmlstr);
            StringAssert.DoesNotContain("all-bundle", htmlstr);

            // Render resources for the rest and verify that we only bundle
            htmlstr = html.RenderResources("all").ToHtmlString();
            StringAssert.Contains("all-bundle", htmlstr);
            StringAssert.DoesNotContain("jqueryui-bundle", htmlstr);
        }

        [Test]
        public void TestGroupRenderingNonExitingGroup()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.ResourceSettings(new HTMLResourceOptions(){ Strict = true });
            try
            {
                var htmlstr = html.RenderResources("thisdoesnotexist").ToHtmlString();
                Assert.Fail("Expected an exception, but none was thrown");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("Can not find resource group thisdoesnotexist", ex.Message);
            }
        }

    }
}
