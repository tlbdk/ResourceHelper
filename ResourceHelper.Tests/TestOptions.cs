﻿using System;
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

// TODO: Support settings options, Html.Resource("~/Content/themes/base/jquery.ui.all.css", ResourceOptions) // (path, bundle, minifiy, CDN) will overwrite configuration
// TODO: Support CSS image inlining, config option ResourceInline=Size in bytes

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestOptions
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
        public void TestNoBundleNoMinify()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            ConfigurationManager.AppSettings["ResourceBundle"] = "false";
            html.Resource("~/Content/Site.css", new ResourceOptions() { Bundle = false, Minify = false });
            StringAssert.StartsWith("<link href=\"/Content/Site.css", html.RenderResources().ToHtmlString());
        }
        */
    }
}