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

namespace ResourceHelper
{
    public class HtmlResources
    {
        public Dictionary<int, List<string>> Scripts;
        public Dictionary<int, List<string>> Stylesheets;

        public bool Bundle = false;
        public bool Minify = false;

        public DateTime LatestScriptFile = DateTime.MinValue;
        public DateTime LatestCSSFile = DateTime.MinValue;

        public HtmlResources()
        {
            Scripts = new Dictionary<int, List<string>>();
            Stylesheets = new Dictionary<int, List<string>>();
            bool.TryParse(ConfigurationManager.AppSettings["ResourceBundle"], out Bundle);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceMinify"], out Minify);
        }
    }

    public static class HtmlHelperExtensions
    {
        private static string scriptsFolder = "~/Scripts/"; 
        private static string cssFolder = "~/Content/"; 

        public static MvcHtmlString Resource(this HtmlHelper html, string value)
        {
            int depth = GetDepth(html);
            //throw new Exception(layout);
            if (ConfigurationManager.AppSettings["ScriptsFolder"] != null)
            {
                scriptsFolder = ConfigurationManager.AppSettings["ScriptsFolder"];
            }
            if (ConfigurationManager.AppSettings["CSSFolder"] != null)
            {
                cssFolder = ConfigurationManager.AppSettings["CSSFolder"];
            }

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
                    // Ensure that list exists.
                    if (!resources.Scripts.Keys.Contains(depth))
                    {
                        resources.Scripts.Add(depth, new List<string>());
                    }
                    if (!resources.Scripts[depth].Contains(value))
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
                                resources.Scripts[depth].Add(value);
                            }
                            else if (File.Exists(server.MapPath(scriptsFolder + origname + ".min" + info.Extension)) && DateTime.Compare(File.GetLastWriteTime(server.MapPath(scriptsFolder + origname + ".min" + info.Extension)), info.LastWriteTime) >= 0)
                            {
                                if (DateTime.Compare(resources.LatestScriptFile, info.LastWriteTime) < 0)
                                {
                                    resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(scriptsFolder + origname + ".min" + info.Extension));
                                }
                                // We have already minified the file. Skip.
                                resources.Scripts[depth].Add(scriptsFolder + origname + ".min" + info.Extension);
                            }
                            else
                            {
                                // Minify file.
                                string filename = scriptsFolder + origname + ".min" + info.Extension;
                                File.WriteAllText(server.MapPath(filename), Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(server.MapPath(value))));
                                resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(filename));

                                // Insert the path to the minified file.
                                resources.Scripts[depth].Add(filename);
                            }
                        }
                        else
                        {
                            resources.Scripts[depth].Add(value);
                        }
                    }
                }
                else if (value.EndsWith(".css"))
                {
                    // ENsure that list exists.
                    if (!resources.Stylesheets.Keys.Contains(depth))
                    {
                        resources.Stylesheets.Add(depth, new List<string>());
                    }
                    if (!resources.Stylesheets[depth].Contains(value))
                    {
                        // Note the latest date a file was changed.
                        if (DateTime.Compare(resources.LatestCSSFile, info.LastWriteTime) < 0)
                            resources.LatestCSSFile = info.LastWriteTime;

                        resources.Stylesheets[depth].Add(value);
                    }
                }
            }
            else
            {
                throw new FileNotFoundException(String.Format("Could not find file {0}", value), value);
            }
            return null;
        }

        private static int GetDepth(HtmlHelper html)
        {
            // Handle resources for razor views.
            if (html.ViewDataContainer is WebPageBase)
            {
                return ((WebPageBase)html.ViewDataContainer).OutputStack.Count;
            }
            // Handle aspx views.
            else if (html.ViewDataContainer is ViewPage)
            {
                return 0;// throw new Exception("What to do?");
            }
            else
            {
                throw new NotSupportedException("The current viewengine is not supported.");
            }
        }

        public static MvcHtmlString RenderResources(this HtmlHelper html)
        {
            if (ConfigurationManager.AppSettings["ScriptsFolder"] != null)
            {
                scriptsFolder = ConfigurationManager.AppSettings["ScriptsFolder"];
            }
            if (ConfigurationManager.AppSettings["CSSFolder"] != null)
            {
                cssFolder = ConfigurationManager.AppSettings["CSSFolder"];
            }

            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;
            var resources = (HtmlResources)html.ViewData["Resources"];
            string result = "";

            if (resources != null)
            {
                // Create ordered lists of scripts and stylesheets.
                IEnumerable<int> scriptKeys = resources.Scripts.Keys.OrderByDescending(k => k).AsEnumerable();
                var _scripts = new List<string>();
                foreach (int key in scriptKeys)
                {
                    foreach (string script in resources.Scripts[key])
                    {
                        _scripts.Add(script);
                    }
                }
                IEnumerable<int> styleKeys = resources.Stylesheets.Keys.OrderByDescending(k => k).AsEnumerable();
                var _styles = new List<string>();
                foreach (int key in styleKeys)
                {
                    foreach (string style in resources.Stylesheets[key])
                    {
                        _styles.Add(style);
                    }
                }

                if (resources.Bundle)
                {
                    if (_scripts.Count > 0)
                    {
                        // Get a hash of the files in question and generate a path.
                        string scriptPath = scriptsFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", _scripts)))).Replace("-", "").ToLower() + ".js";
                        BundleFiles(server, resources.LatestScriptFile, _scripts, scriptPath);
                        result += "<script src=\"" + url.Content(scriptPath) + "?" + String.Format("{0:yyyyMMddHHmmss}", File.GetLastWriteTime(scriptPath)) + "\" type=\"text/javascript\"></script>\n";
                    }

                    if (_styles.Count > 0)
                    {
                        // Get a hash of the files in question and generate a path.
                        string cssPath = cssFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", _styles)))).Replace("-", "").ToLower() + ".css";
                        BundleFiles(server, resources.LatestCSSFile, _styles, cssPath);
                        result += "<link href=\"" + url.Content(cssPath) + "?" + String.Format("{0:yyyyMMddHHmmss}", File.GetLastWriteTime(cssPath)) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
                    }
                }
                else
                {
                    foreach (string resource in _styles)
                    {
                        DateTime dt = File.GetLastWriteTime(server.MapPath(resource));
                        result += "<link href=\"" + url.Content(resource) + "?" + String.Format("{0:yyyyMMddHHmmss}", dt) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
                    }
                    foreach (string resource in _scripts)
                    {
                        DateTime dt = File.GetLastWriteTime(server.MapPath(resource));
                        result += "<script src=\"" + url.Content(resource) + "?" + String.Format("{0:yyyyMMddHHmmss}", dt) + "\" type=\"text/javascript\"></script>\n";
                    }
                }
            }

            html.ViewData["Resources"] = new HtmlResources();
            return MvcHtmlString.Create(result);
        }

        private static void BundleFiles(HttpServerUtilityBase server, DateTime latest, List<string> files, string path)
        {
            if (File.Exists(path) && DateTime.Compare(File.GetLastWriteTime(path), latest) >= 0)
            {
                // We have already bundled the files.
            }
            else
            {
                File.Delete(server.MapPath(path));
                using (var writer = File.CreateText(server.MapPath(path)))
                {
                    foreach (string script in files)
                    {
                        writer.Write(File.ReadAllText(server.MapPath(script)) + ";\n\n");
                    }
                }
            }
        }
    }
}
