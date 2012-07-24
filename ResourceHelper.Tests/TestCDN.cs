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

//
// TODO: Support pointing to a CDN for jquery includes, config option ResourceCDN=Google|Microsoft
//

namespace ResourceHelper.Tests
{
    [TestFixture]
    public class TestCDN
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
