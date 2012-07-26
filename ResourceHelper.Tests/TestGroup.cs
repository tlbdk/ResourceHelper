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


        // TODO: Support Html.ResourceGroup("jquery", "~/Content/themes/base", @"^jquery\.ui.*\.css$") // This index all the resources in base that maches the regex and creates a bundle out of them
        //               Html.ResourceGroup("jquery", "~/Content/themes/base", @"^jquery\.ui.*\.css$", true) // Recurse into folder
        //               Html.Resource("~/Content/themes/base/jquery.ui.all.css") // Will make sure jquery.ui.all.css is first in the bundle
        [Test]
        public void TestGroupCSS()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.ResourceGroup("jqueryui", "~/Content/themes/base/*.css"); // jquery.ui bundle
            html.Resource("~/Content/themes/base/jquery.ui.core.css", new HTMLResourceOptions() { Bundle = true, Minify = false });
            html.Resource("~/Content/Site.css", new HTMLResourceOptions() { Bundle = false, Minify = false });
            //FIXME: Implement that RenderResources uses groups
            var htmlstr = html.RenderResources("jqueryui").ToHtmlString();
            //StringAssert.StartsWith("<link href=\"/Content/cache/jqueryui-bundle", htmlstr);

            htmlstr = html.RenderResources().ToHtmlString();
            //StringAssert.StartsWith("<link href=\"/Content/Site.css", htmlstr);
            
        }
    }
}
