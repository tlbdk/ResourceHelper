using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Web;
using System.Web.Mvc;
using ResourceHelper;
using System.Web.Routing;

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestFixture1
    {
        private HtmlHelper html;
        [SetUp]
        public void Init()
        {
            ViewContext viewContext = new ViewContext();
            viewContext.HttpContext = new FakeHttpContext();
            viewContext.HttpContext.Items.Add("Resources", "foo");

            /* viewContext.RequestContext = new RequestContext();
            viewContext.RequestContext.HttpContext = viewContext.HttpContext; */

            html = new HtmlHelper(viewContext, new FakeViewDataContainer());
        }

        [Test]
        public void TestTrue()
        {
            html.Resource("Test");
        }
    }
}
