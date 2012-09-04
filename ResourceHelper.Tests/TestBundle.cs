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
using System.Text.RegularExpressions;

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestBundle
    {
        string WebRoot;
       
        [SetUp]
        public void Init()
        {
            // Set sample as root
            WebRoot = FakeUtils.GetWebRoot(TestContext.CurrentContext.TestDirectory);
            Directory.SetCurrentDirectory(WebRoot);
            FakeUtils.CleanupCache(WebRoot);
        }

        [Test]
        public void TestBundleConcurrency()
        {
            // Create a bundle file so we know the filename
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            var bundlefile = createbundle(html);
            
            // Create a locked file where the bundle should be be to provoke an error 
            html = FakeUtils.CreateHtmlHelper(WebRoot);
            File.SetLastWriteTime(bundlefile, new DateTime(1981, 1, 1, 0, 0, 1));

            try
            {
                using (var writer = File.OpenWrite(bundlefile))
                {
                    createbundle(html);
                }
                Assert.Fail("Expected an exception, but none was thrown");
            }
            catch (Exception ex)
            {
               StringAssert.StartsWith("Could not write", ex.Message);
            }
        }

        private string createbundle(HtmlHelper html)
        {
            
            var server = html.ViewContext.RequestContext.HttpContext.Server;
            html.ResourceSettings(new HTMLResourceOptions() { Bundle = true, BundleTimeout = 1 });
            html.Resource("~/Content/Test.css");
            html.Resource("~/Content/themes/base/jquery.ui.dialog.css");

            var htmlstr = html.RenderResources().ToHtmlString();
            return server.MapPath("~" + Regex.Match(htmlstr, @"href=""([^\?]+)").Groups[1]);
        }

    }
}
