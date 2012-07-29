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

// TODO: Support Html.Resource("~/Content/themes/base/*.css") // Glob pattern

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestGlob
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
        public void TestSimpleGlob()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.Resource("~/Content/*.css"); // Use Directory.GetFiles("*.exe")
            var htmlstr = html.RenderResources().ToHtmlString();
            StringAssert.StartsWith("<link href=\"/Content/Site.css", htmlstr);
            StringAssert.Contains("/Content/Test.css", htmlstr);
        }
        
        [Test]
        public void TestGlobRecursive()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.Resource("~/Content/*.css", true);
            var htmlstr = html.RenderResources().ToHtmlString();
            StringAssert.StartsWith("<link href=\"/Content/Site.css", htmlstr);
            StringAssert.Contains("/Content/themes/base/jquery.ui.theme.css", htmlstr);
        }
    }
}
