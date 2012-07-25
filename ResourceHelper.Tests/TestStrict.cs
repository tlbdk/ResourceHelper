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
    public class TestStrict
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
        public void TestStrictFileNotFound()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            try
            {
                html.Resource("~/Content/doesnotexists");
                Assert.Fail("Expected an exception, but none was thrown");
            }
            catch (FileNotFoundException ex)
            {
                Assert.IsInstanceOf(typeof(FileNotFoundException), ex);
            }
        }

        [Test]
        public void TestStrictFileFound()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            ConfigurationManager.AppSettings["ResourceBundle"] = "false"; //FIXME, this won't work as it's will be readonly
            html.Resource("~/Content/Site.css", new HTMLResourceOptions() { Bundle = false, Minify = false } );
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }
    }
}
