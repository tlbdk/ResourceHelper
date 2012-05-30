using System;
using System.Web;
using System.Collections.Generic;

namespace ResourceHelper.Sample
{
    public class DebugModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.PostAcquireRequestState += new EventHandler(OnPostAcquireRequestState);
        }

        public void OnPostAcquireRequestState(Object source, EventArgs e)
        {
            var app = (HttpApplication)source;

            // Debug settings for ResourceHelper.
            if (!string.IsNullOrEmpty(app.Request.Params["ResourceHelper.NoMinifying"]) && app.Request.Params["ResourceHelper.NoMinifying"].Equals("enable"))
            {
                app.Context.Session["ResourceHelper.NoMinifying"] = "true";
            }
            if (!string.IsNullOrEmpty(app.Request.Params["ResourceHelper.NoMinifying"]) && app.Request.Params["ResourceHelper.NoMinifying"].Equals("disable"))
            {
                app.Context.Session.Remove("ResourceHelper.NoMinifying");
            }

            if (!string.IsNullOrEmpty(app.Request.Params["ResourceHelper.NoBundling"]) && app.Request.Params["ResourceHelper.NoBundling"].Equals("enable"))
            {
                app.Context.Session["ResourceHelper.NoBundling"] = "true";
            }
            if (!string.IsNullOrEmpty(app.Request.Params["ResourceHelper.NoBundling"]) && app.Request.Params["ResourceHelper.NoBundling"].Equals("disable"))
            {
                app.Context.Session.Remove("ResourceHelper.NoBundling");
            }
        }

        public void Dispose()
        {
            //clean-up code here.
        }
    }
}