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

// TODO: Support Html.Resource("~/Content/themes/base", "^.*\.css$") // Regex pattern
// TODO: Support Html.Resource("~/Content/themes/base", "^.*\.css$", true) // Regex pattern with recurse

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestRegex
    {
        string WebRoot = Environment.GetEnvironmentVariable("RHWEBROOT");
       
        [SetUp]
        public void Init()
        {
            // Set sample as root
            Directory.SetCurrentDirectory(WebRoot);       
        }

        /*
        [Test]
        public void TestRegex()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content", @".*\.css$");
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestRegexRecursive(WebRoot)
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper();
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content", @".*\.css$", true);
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }
        */
    }
}
