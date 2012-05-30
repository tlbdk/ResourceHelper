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
using System.Threading;

/* Links
 *   Nice alternative with more features, but ugly syntax: https://github.com/jetheredge/SquishIt
 */

namespace ResourceHelper
{
    public class HtmlResources
    {
        public Dictionary<int, List<string>> Scripts;
        public Dictionary<int, List<string>> Stylesheets;
        public Dictionary<string, string> PathOffset;

        public bool Bundle = false;
        public bool Minify = false;
        public bool Debug = false;
        public bool Strict = false;

        public DateTime LatestScriptFile = DateTime.MinValue;
        public DateTime LatestCSSFile = DateTime.MinValue;

        public HtmlResources()
        {
            Scripts = new Dictionary<int, List<string>>();
            Stylesheets = new Dictionary<int, List<string>>();
            PathOffset = new Dictionary<string, string>();
            bool.TryParse(ConfigurationManager.AppSettings["ResourceBundle"], out Bundle);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceMinify"], out Minify);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceDebug"], out Debug);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceStrict"], out Strict);
        }
    }

    public static class HtmlHelperExtensions
    {
        private static string scriptsFolder = "~/Scripts/";
        private static string cssFolder = "~/Content/";

        // TODO: Add support for value of *.js or *.css
        public static MvcHtmlString Resource(this HtmlHelper html, string value)
        {
            int depth = GetDepth(html);
            //throw new Exception(layout);
            if (ConfigurationManager.AppSettings["ScriptsFolder"] != null)
            {
                scriptsFolder = ConfigurationManager.AppSettings["ScriptsFolder"];
            }
            if (ConfigurationManager.AppSettings["StyleSheetFolder"] != null)
            {
                cssFolder = ConfigurationManager.AppSettings["StyleSheetFolder"];
            }

            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;
            var resources = (HtmlResources)html.ViewData["Resources"];
            if (resources == null)
            {
                resources = new HtmlResources();
                html.ViewData["Resources"] = resources;
            }

            // Make sure the paths exist
            Directory.CreateDirectory(server.MapPath(scriptsFolder));
            Directory.CreateDirectory(server.MapPath(cssFolder));

            FileInfo info = new FileInfo(server.MapPath(value));
            if (info.Exists)
            {
                // Find the path diffrence so we can fix up included resources in fx css
                resources.PathOffset[value] = GetPathOffset(url.Content(value.Substring(0, value.Length - info.Name.Length)), url.Content(scriptsFolder));

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
                        if (resources.Minify && string.IsNullOrEmpty((string)html.ViewContext.HttpContext.Session["ResourceHelper.NoMinifying"]))
                        {
                            string origname = info.Name.Substring(0, info.Name.LastIndexOf('.'));
                            if (origname.EndsWith(".min"))
                            {
                                // The resource is pre-minified. Skip.
                                resources.Scripts[depth].Add(value);
                            }
                            else if (!resources.Debug && File.Exists(server.MapPath(scriptsFolder + origname + ".min" + info.Extension)) && DateTime.Compare(File.GetLastWriteTime(server.MapPath(scriptsFolder + origname + ".min" + info.Extension)), info.LastWriteTime) >= 0)
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
                                // TODO: Try to fix up relative paths if we move the script file to another location
                                // Minify file.
                                string filename = scriptsFolder + origname + ".min" + info.Extension;
                                MinifyFile(server.MapPath(filename), server.MapPath(value));
                                //File.WriteAllText(server.MapPath(filename), Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(server.MapPath(value))));
                                resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(filename));

                                // Insert the path to the minified file.
                                resources.Scripts[depth].Add(filename);

                                // File changed named because we are using the mimified version 
                                resources.PathOffset[filename] = resources.PathOffset[value];
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
                if (resources.Strict)
                {
                    throw new FileNotFoundException(String.Format("Could not find file {0}", value), value);
                }
            }
            return null;
        }

        private static void MinifyFile(string newpath, string oldpath)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    File.WriteAllText(newpath, Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(oldpath)));
                    break;
                }
                catch (IOException ex)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static string GetPathOffset(string orgpath, string newpath) {
            var uri_orginal = new Uri("http://somehost" + orgpath);
            var uri_new = new Uri("http://somehost" + newpath);
            return uri_new.MakeRelativeUri(uri_orginal).ToString();
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

                // Force bundle and mimify rebuild
                if (resources.Debug)
                {
                    resources.LatestScriptFile = DateTime.Now;
                    resources.LatestCSSFile = DateTime.Now;
                }

                if (resources.Bundle && string.IsNullOrEmpty((string)html.ViewContext.HttpContext.Session["ResourceHelper.NoBundling"]))
                {                  
                    if (_scripts.Count > 0)
                    {
                        // Get a hash of the files in question and generate a path.
                        string scriptPath = scriptsFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", _scripts)))).Replace("-", "").ToLower() + ".bundle.js";
                        BundleFiles(server, resources.LatestScriptFile, _scripts, resources.PathOffset, scriptPath, resources.Strict);
                        result += "<script src=\"" + url.Content(scriptPath) + "?" + String.Format("{0:yyyyMMddHHmmss}", File.GetLastWriteTime(server.MapPath(scriptPath))) + "\" type=\"text/javascript\"></script>\n";
                    }

                    if (_styles.Count > 0)
                    {
                        // Get a hash of the files in question and generate a path.
                        string cssPath = cssFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", _styles)))).Replace("-", "").ToLower() + ".bundle.css";
                        BundleFiles(server, resources.LatestCSSFile, _styles, resources.PathOffset, cssPath, resources.Strict);
                        result += "<link href=\"" + url.Content(cssPath) + "?" + String.Format("{0:yyyyMMddHHmmss}", File.GetLastWriteTime(server.MapPath(cssPath))) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
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

        private static void BundleFiles(HttpServerUtilityBase server, DateTime latest, List<string> files, Dictionary<String, String> offset, string output, bool strict)
        {
            if (File.Exists(server.MapPath(output)) && DateTime.Compare(File.GetLastWriteTime(server.MapPath(output)), latest) >= 0)
            {
                // We have already bundled the files.
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(server.MapPath(output));
                        using (var writer = File.CreateText(server.MapPath(output)))
                        {
                            foreach (string file in files.ToArray())
                            {
                                if (file.EndsWith(".css"))
                                {
                                    writer.Write("/*" + file + "*/\n");
                                    writer.Write(cssFixup(Path.GetDirectoryName(server.MapPath(output)), server.MapPath(file), offset[file], strict) + "\n\n");
                                }
                                else
                                {
                                    // TODO: Do something for java script with fixup
                                    writer.Write("/*" + file + "*/\n");
                                    try
                                    {
                                        writer.Write(File.ReadAllText(server.MapPath(file)) + ";\n\n");
                                    }
                                    catch
                                    {
                                        if (strict) throw;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    catch (IOException ex)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        private static string cssFixup(string basepath, string filename, string pathoffset, bool strict)
        {
            var seen = new HashSet<string>();
            var css_fixup = new Regex(@"((?:url\s*\()|(?:@import\s*[""'])|(?:@import\s*url\([""']))([^'""\)]+)(['""\)]+;?)", RegexOptions.Compiled | RegexOptions.Singleline);
            var ccs_comment = new Regex(@"(?!<"")\/\*.+?\*\/(?!"")", RegexOptions.Compiled | RegexOptions.Singleline);
            return cssFixup(css_fixup, ccs_comment, basepath, filename, pathoffset, strict, seen);
        }

        private static string cssFixup(Regex css_fixup, Regex css_comment, string basepath, string filename, string pathoffset, bool strict, HashSet<string> seen)
        {
            var data = File.ReadAllText(filename);
            data = css_comment.Replace(data, ""); // Remove comments
           
            data = css_fixup.Replace(data, match =>
            {
                // Fix url includes with the correct path offset
                if (match.Groups[1].Value.StartsWith("url"))
                {
                    string newpath = pathoffset + match.Groups[2].Value;
                    if (File.Exists(Path.GetFullPath(Path.Combine(basepath, newpath))) || !strict)
                    {
                        return match.Groups[1].Value + newpath + match.Groups[3].Value;
                    }
                    else
                    {
                        throw new FileNotFoundException("The css url include " + match.Value + " was not found at the new relative location " + newpath
                            + " (" + Path.GetFullPath(Path.Combine(basepath, newpath)) + ")", Path.GetFullPath(Path.Combine(basepath, newpath)));
                    }
                }
                // Follow @import statements and include them in the stream
                else if (match.Groups[1].Value.StartsWith("@import"))
                {                   
                   string importedfile = Path.Combine(Path.GetDirectoryName(filename), match.Groups[2].Value);
                   // Make sure we don't loop in the includes
                   if (seen.Add(importedfile))
                   {
                       return "/*" + importedfile + "*/\n" + cssFixup(css_fixup, css_comment, basepath, importedfile, pathoffset, strict, seen);
                   }
                   else
                   {
                       return "";
                   }

                } else {
                    return match.Value;
                }
            });
            return data;
        }
    }
}
