﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Collections;
using System.Web.Mvc;
using System.IO;
using System.Reflection;
using System.Collections.Specialized;
using System.Web.SessionState;

namespace ResourceHelper.Tests
{
    public static class FakeUtils
    {
        public static HtmlHelper CreateHtmlHelper(string WebRoot)
        {
            // Cleanup up cache directoy
            string CacheDir = WebRoot + @"Contant\Cache";
            if (Directory.Exists(CacheDir))
            {
                Directory.Delete(CacheDir, true);
            }
            Directory.CreateDirectory(CacheDir);

            // Create some mock objects to create a context
            ViewContext viewContext = new ViewContext();
            viewContext.HttpContext = new FakeHttpContext();
            return new HtmlHelper(viewContext, new FakeViewDataContainer());
        }
    }

    public class FakeHttpContext : HttpContextBase
    {
        private Dictionary<object, object> _items = new Dictionary<object, object>();

        public override IDictionary Items { get { return _items; } }

        public override HttpRequestBase Request
        {
            get { return new FakeHttpRequest(); }
        }

        public override HttpResponseBase Response
        {
            get { return new FakeHttpResponse(); }
        }

        public override HttpServerUtilityBase Server
        {
            get { return new FakeHttpServerUtility(); }
        }

        public override HttpSessionStateBase Session
        {
            get { return new FakeHttpSession(); }
        }
    }

    internal sealed class FakeHttpSession : HttpSessionStateBase
    {
        private readonly NameValueCollection objects = new NameValueCollection();

        public override object this[string name]
        {
            get { return (objects.AllKeys.Contains(name)) ? objects[name] : null; }
            set { objects[name] = (string)value; }
        }

        public override NameObjectCollectionBase.KeysCollection Keys
        {
            get { return objects.Keys; }
        }
    }

    public class FakeViewDataContainer : IViewDataContainer
    {
        private ViewDataDictionary _viewData = new ViewDataDictionary();
        public ViewDataDictionary ViewData { get { return _viewData; } set { _viewData = value; } }
    }

    public class FakeHttpServerUtility : HttpServerUtilityBase
    {
        public override string MapPath(string path)
        {
            return Directory.GetCurrentDirectory() + path.Replace("~", "").Replace('/', '\\');
        }
    }

    public class FakeHttpRequest : HttpRequestBase
    {
        public override NameValueCollection ServerVariables
        {
            get { return new NameValueCollection(); }
        }


        public override string AppRelativeCurrentExecutionFilePath
        {
            get { return "~/"; }
        }

        public override string PathInfo
        {
            get { return ""; }
        }

        public override string ApplicationPath
        {
            get { return "/"; }
        }
    }

    public class FakeHttpResponse : HttpResponseBase
    {
        public override string ApplyAppPathModifier(string path) {
            return path.Replace("~", "/");
        }
         
    }
}