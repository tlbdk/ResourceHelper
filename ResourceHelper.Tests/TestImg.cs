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
    public class TestImg
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
        [Ignore("TODO: Implement Image() support")]
        public void TestImgResource()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.ResourceSettings(new HTMLResourceOptions() { Strict = true });
            var imgpath = html.Image("~/Content/images/sample.png").ToHtmlString();
        }
    }
}
