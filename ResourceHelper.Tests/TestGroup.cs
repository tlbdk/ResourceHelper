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

// Links: http://www.hanselman.com/blog/NuGetPackageOfTheWeek1ASPNETSpriteAndImageOptimization.aspx
// TODO: Support Html.ResourceGroup("~/Content/widget-small") // This index all the resource in widget-small and create sprite out of the images in the folder
//               Html.Resource("~/Content/widget-small/news.gif") // This creates an image tag pointing to the sprite, use Cache to store lookup map

// TODO: Support Html.ResourceGroup("~/Content/themes/base", @"^jquery\.ui.*\.css$") // This index all the resources in base that maches the regex and creates a bundle out of them
//               Html.ResourceGroup("~/Content/themes/base", @"^jquery\.ui.*\.css$", true) // Recurse into folder
//               Html.Resource("~/Content/themes/base/jquery.ui.all.css") // Will make sure jquery.ui.all.css is first in the bundle

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestGroup
    {
        string WebRoot = Environment.GetEnvironmentVariable("RHWEBROOT");
       
        [SetUp]
        public void Init()
        {
            // Set sample as root
            Directory.SetCurrentDirectory(WebRoot);       
        }

    }
}
