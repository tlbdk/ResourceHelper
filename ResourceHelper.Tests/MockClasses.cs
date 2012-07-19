using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections;
using System.Web.Mvc;
using System.IO;
using System.Reflection;

namespace ResourceHelper.Tests
{
    public class FakeHttpContext : HttpContextBase
    {
        private Dictionary<object, object> _items = new Dictionary<object, object>();

        public override IDictionary Items { get { return _items; } }

        public override HttpRequestBase Request
        {
            get { return new FakeHttpRequest(); }
        }

        public override HttpServerUtilityBase Server
        {
            get { return new FakeHttpServerUtility(); }
        }
    }

    public class FakeViewDataContainer : IViewDataContainer
    {
        private ViewDataDictionary _viewData = new ViewDataDictionary();
        public ViewDataDictionary ViewData { get { return _viewData; } set { _viewData = value; } }
    }

    public class FakeHttpServerUtility : HttpServerUtilityBase
    {
        string ServerRoot = Directory.GetCurrentDirectory();

        public override string MapPath(string path)
        {
            var test = ServerRoot + path.Replace("~", null).Replace('/', '\\');
            return test;
        }
    }

    public class FakeHttpRequest : HttpRequestBase
    {
        public override string AppRelativeCurrentExecutionFilePath
        {
            get { return "~/"; }
        }

        public override string PathInfo
        {
            get { return ""; }
        }
    }


}
