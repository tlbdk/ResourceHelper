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
        string WebRoot = Environment.GetEnvironmentVariable("RHWEBROOT");
       
        [SetUp]
        public void Init()
        {
            // Set sample as root
            Directory.SetCurrentDirectory(WebRoot);       
        }

        /*
        [Test]
        public void TestGlob()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/*.css"); // Use Directory.GetFiles("*.exe")
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestGlobRecursive()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/*.css", true); // Use Directory.GetFiles("*.exe")
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }

        [Test]
        public void TestOptions()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/Site.css", new ResourceOptions() { Bundle = false, Minify = false });
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }
        */

    }
}
