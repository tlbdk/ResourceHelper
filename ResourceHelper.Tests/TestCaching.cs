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

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestCaching
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
        //[Ignore("TODO: Implement HttpContext.Cache support")]
        public void TestResourceCaching()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.ResourceSettings(new HTMLResourceOptions() { ContextCacheLifetime = 10 });

            html.Resource("~/Content/Test.css");
            html.Resource("~/Content/themes/base/jquery.ui.dialog.css");
            var htmlstr = html.RenderResources().ToHtmlString();

            html.ViewContext.HttpContext.Cache["test"] = 1;

            // TODO: We should be getting resources from HttpContext.Cache and all file stat's info should also be contained there
            // Do new request and try use the same resources
            html = FakeUtils.CreateHtmlHelper(WebRoot);          
            html.Resource("~/Content/Test.css");
            html.Resource("~/Content/themes/base/jquery.ui.dialog.css");
            htmlstr = html.RenderResources().ToHtmlString();



        }
    }
}
