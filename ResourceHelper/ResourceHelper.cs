using System;
using System.Web.Mvc;
using System.Web.WebPages;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Globalization;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace MvcHtmlHelpers
{
    public class HtmlResources
    {
        public List<string> Scripts;
        public List<string> Stylesheets;

        public bool Bundle = false;
        public bool Minify = false;

        public DateTime LatestScriptFile = DateTime.MinValue;
        public DateTime LatestCSSFile = DateTime.MinValue;

        public HtmlResources()
        {
            Scripts = new List<String>();
            Stylesheets = new List<String>();
            bool.TryParse(ConfigurationManager.AppSettings["ResourceBundle"], out Bundle);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceMinify"], out Minify);
        }
    }

    public static class HtmlHelperExtensions
    {
        private const string scriptsFolder = "~/Scripts/"; // TODO: Get from somewhere else!
        private const string cssFolder = "~/Content/"; // TODO: Get from somewhere else!

        public static MvcHtmlString Resource(this HtmlHelper html, string value)
        {
            var server = html.ViewContext.RequestContext.HttpContext.Server;
            var resources = (HtmlResources)html.ViewData["Resources"];
            if (resources == null)
            {
                resources = new HtmlResources();
                html.ViewData["Resources"] = resources;
            }

            if (File.Exists(server.MapPath(value)))
            {
                FileInfo info = new FileInfo(server.MapPath(value));
                if (value.EndsWith(".js"))
                {
                    if (!resources.Scripts.Contains(value))
                    {
                        // Note the latest date a file was changed.
                        if (DateTime.Compare(resources.LatestScriptFile, info.LastWriteTime) < 0)
                        {
                            resources.LatestScriptFile = info.LastWriteTime;
                        }

                        // Minify the script file if necessary.
                        if (resources.Minify)
                        {
                            string origname = info.Name.Substring(0, info.Name.LastIndexOf('.'));
                            if (origname.EndsWith(".min"))
                            {
                                // The resource is pre-minified. Skip.
                                resources.Scripts.Insert(0, value);
                            }
                            else if (File.Exists(server.MapPath(scriptsFolder + origname + ".min" + info.Extension)) && DateTime.Compare(File.GetLastWriteTime(server.MapPath(scriptsFolder + origname + ".min" + info.Extension)), info.LastWriteTime) >= 0)
                            {
                                if (DateTime.Compare(resources.LatestScriptFile, info.LastWriteTime) < 0)
                                {
                                    resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(scriptsFolder + origname + ".min" + info.Extension));
                                }
                                // We have already minified the file. Skip.
                                resources.Scripts.Insert(0, scriptsFolder + origname + ".min" + info.Extension);
                            }
                            else
                            {
                                // Minify file.
                                string filename = scriptsFolder + origname + ".min" + info.Extension;
                                File.WriteAllText(server.MapPath(filename), Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(server.MapPath(value))));
                                resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(filename));

                                // Insert the path to the minified file.
                                resources.Scripts.Insert(0, filename);
                            }
                        }
                        else
                        {
                            resources.Scripts.Insert(0, value);
                        }
                    }
                }
                else if (value.EndsWith(".css"))
                {
                    if (!resources.Stylesheets.Contains(value))
                    {
                        // Note the latest date a file was changed.
                        if (DateTime.Compare(resources.LatestCSSFile, info.LastWriteTime) < 0)
                            resources.LatestCSSFile = info.LastWriteTime;

                        resources.Stylesheets.Insert(0, value);
                    }
                }
            }
            else
            {
                throw new FileNotFoundException(String.Format("Could not find file {0}", value), value);
            }
            return null;
        }

        public static MvcHtmlString RenderResources(this HtmlHelper html)
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;
            var resources = (HtmlResources)html.ViewData["Resources"];
            string result = "";

            if (resources != null)
            {
                if (resources.Bundle)
                {
                    // Get a hash of the files in question and generate a path.
                    string scriptPath = scriptsFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", resources.Scripts)))) + ".js";
                    if (File.Exists(scriptPath) && DateTime.Compare(File.GetLastWriteTime(scriptPath), resources.LatestScriptFile) >= 0)
                    {
                        // We have already bundled the files.
                    }
                    else
                    {
                        foreach (string script in resources.Scripts)
                        {
                            File.AppendAllText(server.MapPath(scriptPath), File.ReadAllText(server.MapPath(script)));
                        }
                    }
                    result += "<script src=\"" + url.Content(scriptPath) + "?" + String.Format("{0:yyyyddHHss}", File.GetLastWriteTime(scriptPath)) + "\" type=\"text/javascript\"></script>\n";

                    // Get a hash of the files in question and generate a path.
                    string cssPath = cssFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", resources.Stylesheets)))) + ".css";
                    if (File.Exists(cssPath) && DateTime.Compare(File.GetLastWriteTime(cssPath), resources.LatestCSSFile) >= 0)
                    {
                        // We have already bundled the files.
                    }
                    else
                    {
                        foreach (string stylesheet in resources.Stylesheets)
                        {
                            File.AppendAllText(server.MapPath(cssPath), File.ReadAllText(server.MapPath(stylesheet)));
                        }
                    }
                    result += "<link href=\"" + url.Content(cssPath) + "?" + String.Format("{0:yyyyddHHss}", File.GetLastWriteTime(cssPath)) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
                }
                else
                {
                    foreach (string resource in resources.Stylesheets)
                    {
                        DateTime dt = File.GetLastWriteTime(server.MapPath(resource));
                        result += "<link href=\"" + url.Content(resource) + "?" + String.Format("{0:yyyyddHHss}", dt) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
                    }
                    foreach (string resource in resources.Scripts)
                    {
                        DateTime dt = File.GetLastWriteTime(server.MapPath(resource));
                        result += "<script src=\"" + url.Content(resource) + "?" + String.Format("{0:yyyyddHHss}", dt) + "\" type=\"text/javascript\"></script>\n";
                    }
                }
            }

            return MvcHtmlString.Create(result);
        }
    }
}
