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
    public class TestUtility
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
        [Ignore("TODO: Implement RenderResourceList")]
        public void TestUtilityLink()
        {
            HtmlHelper html = FakeUtils.CreateHtmlHelper(WebRoot);
            html.Resource("~/Scripts/modernizr-1.7.js");
            html.Resource("~/Scripts/jquery.validate.js");
            var list = html.RenderResourceList(); // Returns a list of urls that will be rendered in the next call to RenderResources()
            Assert.That( new List<string>() { "/Scripts/modernizr-1.7.js", "Scripts/jquery.validate.js"}, Is.EquivalentTo( list ) );
            html.RenderResources();
        }
    }
}
