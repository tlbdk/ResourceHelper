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
    public class TestFixture1
    {
        public string WebRoot = @"C:\Users\Troels Liebe Bentsen\Desktop\ResourceHelper\ResourceHelper.Sample\";
        [SetUp]
        public void Init()
        {
            // Set sample as root
            Directory.SetCurrentDirectory(WebRoot);       
        }

        [Test]
        public void TestStrictFileNotFound()
        {
            HtmlHelper html = CreateHtmlHelper();
            try
            {
                html.Resource("~/Content/doesnotexists");
                Assert.Fail("Expected an exception, but none was thrown");
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf(typeof(FileNotFoundException), ex);
            }
        }

        [Test]
        public void TestStrictFileFound()
        {
            HtmlHelper html = CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/Site.css");
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestGlob()
        {
            HtmlHelper html = CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/*.css"); // Use Directory.GetFiles("*.exe")
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestGlobRecursive()
        {
            HtmlHelper html = CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/*.css", true); // Use Directory.GetFiles("*.exe")
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestRegex()
        {
            HtmlHelper html = CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content", @".*\.css$");
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestRegexRecursive()
        {
            HtmlHelper html = CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content", @".*\.css$", true);
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestOptions()
        {
            HtmlHelper html = CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/Site.css", new ResourceOptions() { Bundle = false, Minify = false });
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        public HtmlHelper CreateHtmlHelper()
        {
            // Cleanup up cache directoy
            string CacheDir = WebRoot + @"Contant\Cache";
            if (Directory.Exists(CacheDir))
            {
                Directory.Delete(CacheDir, true);
            }
            Directory.CreateDirectory(CacheDir);

            // Create some mock objects to create a context
            ViewContext viewContext = new ViewContext();
            viewContext.HttpContext = new FakeHttpContext();
            return new HtmlHelper(viewContext, new FakeViewDataContainer());
        }
    }
}
