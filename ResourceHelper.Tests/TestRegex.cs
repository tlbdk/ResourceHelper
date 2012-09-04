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
    public class TestRegex
    {
        string WebRoot;
       
        [SetUp]
        public void Init()
        {
            // Set sample as root
            WebRoot = FakeUtils.GetWebRoot(TestContext.CurrentContext.TestDirectory);
            Directory.SetCurrentDirectory(WebRoot); 
        }

        // TODO: Support Html.Resource("~/Content/themes/base", "^.*\.css$") // Regex pattern
        [Test]
        [Ignore("TODO: Implement regex support in Resource()")]
        public void TestRegexSimple()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.Resource("~/Content", @".*\.css$");
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        // TODO: Support Html.Resource("~/Content/themes/base", "^.*\.css$", true) // Regex pattern with recurse
        [Test]
        [Ignore("TODO: Implement regex support in Resource()")]
        public void TestRegexRecursive()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.Resource("~/Content", @".*\.css$", true);
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }
    }
}
