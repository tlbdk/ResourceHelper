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
    public class HTMLResourceOptions
    {
        public bool? Bundle;
        public bool? Minify;
        public bool? Debug;
        public bool? Strict;
        public int? Inline;
        public string OutputPath;
    }

    public class HtmlResources
    {
        public Dictionary<int, List<string>> Scripts;
        public Dictionary<int, List<string>> Stylesheets;
        public Dictionary<string, string> PathOffset;
        public Dictionary<string, HTMLResourceOptions> Options;
        public Dictionary<string, List<string>> Groups;
        public Dictionary<string, string> GroupsLookup;

        public bool Bundle = false;
        public bool Minify = false;
        public bool Debug = false;
        public bool Strict = false;
        public int Inline = 0;

        public DateTime LatestScriptFile = DateTime.MinValue;
        public DateTime LatestCSSFile = DateTime.MinValue;

        public HtmlResources()
        {
            // We store the script and stylesheets on different levels depending on where they where added in the process so we get the order rigth
            Scripts = new Dictionary<int, List<string>>();
            Stylesheets = new Dictionary<int, List<string>>();
            PathOffset = new Dictionary<string, string>();
            Options = new Dictionary<string, HTMLResourceOptions>();
            Groups = new Dictionary<string, List<string>>();
            GroupsLookup = new Dictionary<string, string>();
            bool.TryParse(ConfigurationManager.AppSettings["ResourceBundle"], out Bundle);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceMinify"], out Minify);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceDebug"], out Debug);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceStrict"], out Strict);
            int.TryParse(ConfigurationManager.AppSettings["ResourceInline"], out Inline);            
        }
    }

    public static class HtmlHelperExtensions
    {
        private static string scriptsFolder = "~/Content/cache/";
        private static string cssFolder = "~/Content/cache/";


        // Simple/Glob: html.ResourceGroup("~/Content/*.css");
        public static MvcHtmlString ResourceGroup(this HtmlHelper html, string name, string path)
        {
            return ResourceGroup(html, name, path, null, false);
        }

        // Glob recursive: html.ResourceGroup("~/Content/*.css");
        public static MvcHtmlString ResourceGroup(this HtmlHelper html, string name, string path, bool recursive)
        {
            return ResourceGroup(html, name, path, null, recursive);
        }

        // Regex recursive: html.ResourceGroup("~/Content/*.css");
        public static MvcHtmlString ResourceGroup(this HtmlHelper html, string name, string path, string regex)
        {
            return ResourceGroup(html, name, path, regex, false);
        }

        // Regex recursive: html.ResourceGroup("~/Content/*.css");
        public static MvcHtmlString ResourceGroup(this HtmlHelper html, string name, string path, string regex, bool recursive)
        {
            var resources = GetResources(html);
            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;

            // Ensure that list exists.
            if (!resources.Groups.Keys.Contains(name))
            {
                resources.Groups.Add(name, new List<string>());
            }

            FileInfo[] files = null;

            if (regex != null)
            {
                // TODO: Implement
            }
            // Try to glob for files if it's not a regular expression
            else
            {
                var di = new DirectoryInfo(Path.GetDirectoryName(server.MapPath(path)));
                files = di.GetFiles(Path.GetFileName(path), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            foreach (var file in files)
            {
                var aspurl = MapPathReverse(html, file.FullName);
                if (!resources.GroupsLookup.ContainsKey(aspurl))
                {
                    resources.Groups[name].Add(aspurl);
                    resources.GroupsLookup[aspurl] = name;
                }
                else
                {
                    throw new Exception("Can't add the same resource '" + aspurl + "' to two different groups: " + name + " and " + resources.GroupsLookup[aspurl]);
                }
            }

            return null;
        }

        // Simple options: html.Resource("~/Content/Site.css", new ResourceOptions() { Bundle = false });
        public static MvcHtmlString Resource(this HtmlHelper html, string value, HTMLResourceOptions options)
        {
            return Resource(html, value, null, false, options);
        }

        // Simple: html.Resource("~/Content/Site.css");
        // Glob: html.Resource("~/Content/*.css");
        public static MvcHtmlString Resource(this HtmlHelper html, string value)
        {
            return Resource(html, value, null, false, null);
        }

        // Glob recursive: html.Resource("~/Content/*.css", true);
        public static MvcHtmlString Resource(this HtmlHelper html, string value, bool recursive)
        {
            return Resource(html, value, null, recursive, null);
        }

        // Glob recursive options: html.Resource("~/Content/*.css", true, new ResourceOptions() { Bundle = false });
        public static MvcHtmlString Resource(this HtmlHelper html, string value, bool recursive, HTMLResourceOptions options)
        {
            return Resource(html, value, null, recursive, options);
        }

        // Regex: html.Resource("~/Content/", @"*\.css$");
        public static MvcHtmlString Resource(this HtmlHelper html, string value, string regex)
        {
            return Resource(html, value, regex, false, null);
        }

        // Regex recursive: html.Resource("~/Content/", @"*\.css$", true);
        public static MvcHtmlString Resource(this HtmlHelper html, string value, string regex, bool recursive)
        {
            return Resource(html, value, regex, recursive, null);
        }

        public static MvcHtmlString Resource(this HtmlHelper html, string value, string regex, bool recursive, HTMLResourceOptions options)
        {
            var resources = GetResources(html);

            // TODO: Return cached copy here so we don't need to do more code parsing

            // Set defaults
            if (ConfigurationManager.AppSettings["ScriptsFolder"] != null)
            {
                scriptsFolder = ConfigurationManager.AppSettings["ScriptsFolder"];
            }
            if (ConfigurationManager.AppSettings["StyleSheetFolder"] != null)
            {
                cssFolder = ConfigurationManager.AppSettings["StyleSheetFolder"];
            }

            // Find out how deep we are in the page structure so we add the resources in the right order 
            int depth = GetDepth(html);

            // Get helper class to convert path's
            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;

            // Make sure the paths exist
            Directory.CreateDirectory(server.MapPath(scriptsFolder));
            Directory.CreateDirectory(server.MapPath(cssFolder));

            FileInfo[] files = null;

            if (regex != null)
            {
                // TODO: Implement
            }
            // Try to glob for files if it's not a regular expression
            else
            {
                var di = new DirectoryInfo(Path.GetDirectoryName(server.MapPath(value)));
                //TODO: This does not work
                files = di.GetFiles(Path.GetFileName(server.MapPath(value)), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            // Throw exception if we are in strict mode and nothing was found
            if (files.Length == 0 && ((resources.Strict && resources.Strict != false) || resources.Strict == true))
            {
                throw new FileNotFoundException(String.Format("Could not find file {0}", value), value);
            }

            foreach (var info in files)
            {
                // Find the Relative URL path 
                var relative_filename = MapPathReverse(html, info.FullName);

                // Skip cache folders
                if (relative_filename.StartsWith(scriptsFolder) || relative_filename.StartsWith(cssFolder))
                {
                    continue;
                }

                // Find the path diffrence so we can fix up included resources in fx css
                resources.PathOffset[relative_filename] = GetPathOffset(url.Content(relative_filename.Substring(0, relative_filename.Length - info.Name.Length)), url.Content(scriptsFolder));

                // Save options for later use
                resources.Options[relative_filename] = options;

                if (relative_filename.EndsWith(".js"))
                {
                    // Ensure that list exists.
                    if (!resources.Scripts.Keys.Contains(depth))
                    {
                        resources.Scripts.Add(depth, new List<string>());
                    }

                    if (!resources.Scripts[depth].Contains(relative_filename))
                    {
                        // Note the latest date a file was changed.
                        if (DateTime.Compare(resources.LatestScriptFile, info.LastWriteTime) < 0)
                        {
                            resources.LatestScriptFile = info.LastWriteTime;
                        }

                        // Minify the script file if necessary.
                        if ((resources.Minify && options.Minify != false) || options.Minify == true)
                        {
                            string origname = info.Name.Substring(0, info.Name.LastIndexOf('.'));
                            if (origname.EndsWith(".min"))
                            {
                                // The resource is pre-minified. Skip.
                                resources.Scripts[depth].Add(relative_filename);
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
                                MinifyFile(server.MapPath(filename), server.MapPath(relative_filename));
                                //File.WriteAllText(server.MapPath(filename), Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(server.MapPath(value))));
                                resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(filename));

                                // Insert the path to the minified file.
                                resources.Scripts[depth].Add(filename);

                                // File changed named because we are using the mimified version 
                                resources.PathOffset[filename] = resources.PathOffset[relative_filename];
                            }
                        }
                        else
                        {
                            resources.Scripts[depth].Add(relative_filename);
                        }
                    }
                }
                else if (relative_filename.EndsWith(".css"))
                {
                    // ENsure that list exists.
                    if (!resources.Stylesheets.Keys.Contains(depth))
                    {
                        resources.Stylesheets.Add(depth, new List<string>());
                    }
                    if (!resources.Stylesheets[depth].Contains(relative_filename))
                    {
                        // Note the latest date a file was changed.
                        if (DateTime.Compare(resources.LatestCSSFile, info.LastWriteTime) < 0)
                        {
                            resources.LatestCSSFile = info.LastWriteTime;
                        }
                        
                        resources.Stylesheets[depth].Add(relative_filename);
                    }
                }
            }

            return null;
        }

        private static void MinifyFile(string newpath, string oldpath)
        {
            // Try to write the file 5 times before giving up
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    File.WriteAllText(newpath, Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(oldpath)));
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static string GetPathOffset(string orgpath, string newpath)
        {
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
                return 0;
            }
            else
            {
                // Return 0 if we don't know the viewengine
                return 0;
            }
        }

        // TODO: Implement
        public static List<string> RenderResourceList(this HtmlHelper html)
        {
            return null;
        }

        // TODO: Implement
        public static MvcHtmlString RenderResources(this HtmlHelper html)
        {
            return RenderResources(html, "all");
        }

        // TODO: Implement
        public static MvcHtmlString RenderScriptResources(this HtmlHelper html)
        {
           return RenderResources(html, "scripts");
        }

        // TODO: Implement
        public static MvcHtmlString RenderStyleSheetResources(this HtmlHelper html)
        {
            return RenderResources(html, "stylesheets");
        }

        public static MvcHtmlString RenderResources(this HtmlHelper html, string groupname)
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

                if (resources.Bundle)
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

            // Clear Resources
            html.ViewData["Resources"] = null;
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
                    catch (IOException)
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

                }
                else
                {
                    return match.Value;
                }
            });
            return data;
        }

        private static string MapPathReverse(HtmlHelper html, string path)
        {
            return "~/" + path.Replace(html.ViewContext.RequestContext.HttpContext.Request.PhysicalApplicationPath, String.Empty).Replace('\\', '/');
        }

        private static HtmlResources GetResources(HtmlHelper html) {
            // Store state in the ViewData //TODO: Implment some kind of IIS caching so we don't do this for every page load
            var resources = (HtmlResources)html.ViewData["Resources"];
            if (resources == null)
            {
                resources = new HtmlResources();
                html.ViewData["Resources"] = resources;
            }
            return resources;
        }
    }
}
