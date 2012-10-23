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
        public string CacheFolder;
        public string ScriptFolder;
        public string StyleSheetFolder;
        public string ImageFolder;
        // Utility stuff
        public int? BundleTimeout;
        public int? MimifyTimeout;
        public int? ContextCacheLifetime;
    }

    public class HtmlResources
    {
        public Dictionary<int, List<string>> Scripts;
        public Dictionary<int, List<string>> Stylesheets;
        public HashSet<string> Rendered;
        public Dictionary<string, string> PathOffset;
        public Dictionary<string, HTMLResourceOptions> Options;
        public Dictionary<string, List<string>> Groups;
        public Dictionary<string, string> GroupsLookup;
        public HashSet<string> GroupsUsed;
        public int BundleTimeout = 5000;
        public int MimifyTimeout = 5000;
        public int ContextCacheLifetime = 0; // 
        public bool Bundle = false;
        public bool Minify = false;
        public bool Debug = false;
        public bool Strict = false;
        public int Inline = 0;
        public string CacheFolder = "~/Content/cache/";
        public string ScriptFolder;
        public string StyleSheetFolder;
        public string ImageFolder;
        public DateTime LatestScriptFile = DateTime.MinValue;
        public DateTime LatestCSSFile = DateTime.MinValue;

        public HtmlResources()
        {
            // We store the script and stylesheets on different levels depending on where they where added in the process so we get the order rigth
            Scripts = new Dictionary<int, List<string>>();
            Stylesheets = new Dictionary<int, List<string>>();
            Rendered = new HashSet<string>();
            PathOffset = new Dictionary<string, string>();
            Options = new Dictionary<string, HTMLResourceOptions>();
            Groups = new Dictionary<string, List<string>>(); // Group name to list of urls
            GroupsLookup = new Dictionary<string, string>(); // Url to group name
            GroupsUsed = new HashSet<string>();
            bool.TryParse(ConfigurationManager.AppSettings["ResourceBundle"], out Bundle);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceMinify"], out Minify);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceDebug"], out Debug);
            bool.TryParse(ConfigurationManager.AppSettings["ResourceStrict"], out Strict);
            int.TryParse(ConfigurationManager.AppSettings["ResourceInline"], out Inline);
            CacheFolder = ConfigurationManager.AppSettings["ResourceCacheFolder"] != null ? ConfigurationManager.AppSettings["CacheFolder"] : CacheFolder;
            StyleSheetFolder = ConfigurationManager.AppSettings["ResourceStyleSheetFolder"] != null ? ConfigurationManager.AppSettings["StyleSheetFolder"] : CacheFolder;
            ScriptFolder = ConfigurationManager.AppSettings["ResourceScriptsFolder"] != null ? ConfigurationManager.AppSettings["ScriptsFolder"] : CacheFolder;
            ImageFolder = ConfigurationManager.AppSettings["ResourceImageFolder"] != null ? ConfigurationManager.AppSettings["ImageFolder"] : CacheFolder;
        }
    }

    public static class HtmlHelperExtensions
    {
        // Set Default settings
        public static MvcHtmlString ResourceSettings(this HtmlHelper html, HTMLResourceOptions options)
        {
            var resources = GetResources(html);

            if (options.Bundle != null)
            {
                resources.Bundle = (bool)options.Bundle;
            }
            if (options.Debug != null)
            {
                resources.Debug = (bool)options.Debug;
            }
            if (options.Minify != null)
            {
                resources.Minify = (bool)options.Minify;
            }
            if (options.Strict != null)
            {
                resources.Strict = (bool)options.Strict;
            }
            if (options.Inline != null)
            {
                resources.Inline = (int)options.Inline;
            }
            if (options.CacheFolder != null)
            {
                resources.CacheFolder = options.CacheFolder;
                resources.StyleSheetFolder = options.StyleSheetFolder;
                resources.ScriptFolder = options.ScriptFolder;
                resources.ImageFolder = options.ImageFolder;
            }
            if (options.StyleSheetFolder != null)
            {
                resources.StyleSheetFolder = options.StyleSheetFolder;
            }
            if (options.ScriptFolder != null)
            {
                resources.ScriptFolder = options.ScriptFolder;
            }
            if (options.ImageFolder != null)
            {
                resources.ImageFolder = options.ImageFolder;
            }
            if (options.BundleTimeout != null)
            {
                resources.BundleTimeout = (int)options.BundleTimeout;
            }
            if (options.MimifyTimeout != null)
            {
                resources.MimifyTimeout = (int)options.MimifyTimeout;
            }
            if (options.ContextCacheLifetime != null)
            {
                resources.ContextCacheLifetime = (int)options.ContextCacheLifetime;
            }

            // TODO: If debug print settings as commented out HTML
            return null;
        }

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

        // Regex: html.ResourceGroup("all", "~/Content/", @"*\.css");
        public static MvcHtmlString ResourceGroup(this HtmlHelper html, string name, string path, string regex)
        {
            return ResourceGroup(html, name, path, regex, false);
        }

        // Regex recursive: html.ResourceGroup("all", "~/Content/", @"*\.css", true);
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
                // TODO: We need to some better sanitization of the path we get and move this code into a common function used by both ResourceGroup and Resource
                var filename = path.Substring(path.LastIndexOf('/') + 1);
                var asp_path = path.Substring(0, path.LastIndexOf('/') + 1);

                var di = new DirectoryInfo(Path.GetDirectoryName(server.MapPath(asp_path)));
                files = di.GetFiles(Path.GetFileName(filename), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            }

            foreach (var file in files)
            {
                var aspurl = MapPathReverse(html, file.FullName);
                if (!resources.GroupsLookup.ContainsKey(aspurl))
                {
                    if (!InCacheFolder(resources, file.FullName)) {
                        //if resource already in "all", add to group and make group used
                        var allFiles = resources.Stylesheets.Union(resources.Scripts);
                        var depth = allFiles.FirstOrDefault(l => l.Value.Contains(aspurl));

                        if(!depth.Equals(default(KeyValuePair<int, List<string>>)))
                        {
                            depth.Value.Remove(aspurl);
                            if(!resources.GroupsUsed.Contains(name)) resources.GroupsUsed.Add(name);
                        }

                        resources.Groups[name].Add(aspurl);
                        resources.GroupsLookup[aspurl] = name;
                    }
                }
            }

            return null;
        }

        private static bool InCacheFolder(HtmlResources resources, string file) {

            // Check if the file is contained in any of the cache folders
            if (file.StartsWith(resources.StyleSheetFolder) || file.StartsWith(resources.ScriptFolder) || file.StartsWith(resources.ImageFolder))
            {
                return true;
            }
            else 
            {
                return false;
            }
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
            return Resource(html, value, null, false, new HTMLResourceOptions());
        }

        // Glob recursive: html.Resource("~/Content/*.css", true);
        public static MvcHtmlString Resource(this HtmlHelper html, string value, bool recursive)
        {
            return Resource(html, value, null, recursive, new HTMLResourceOptions());
        }

        // Glob recursive options: html.Resource("~/Content/*.css", true, new ResourceOptions() { Bundle = false });
        public static MvcHtmlString Resource(this HtmlHelper html, string value, bool recursive, HTMLResourceOptions options)
        {
            return Resource(html, value, null, recursive, options);
        }

        // Regex: html.Resource("~/Content/", @"*\.css$");
        public static MvcHtmlString Resource(this HtmlHelper html, string value, string regex)
        {
            return Resource(html, value, regex, false, new HTMLResourceOptions());
        }

        // Regex recursive: html.Resource("~/Content/", @"*\.css$", true);
        public static MvcHtmlString Resource(this HtmlHelper html, string value, string regex, bool recursive)
        {
            return Resource(html, value, regex, recursive, new HTMLResourceOptions());
        }

        public static MvcHtmlString Resource(this HtmlHelper html, string value, string regex, bool recursive, HTMLResourceOptions options)
        {
            var resources = GetResources(html);

            // TODO: Return cached copy here so we don't need to do more code parsing

            // Find out how deep we are in the page structure so we add the resources in the right order
            int depth = GetDepth(html);

            // Get helper class to convert path's
            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;

            // Make sure the paths exist
            Directory.CreateDirectory(server.MapPath(resources.ScriptFolder));
            Directory.CreateDirectory(server.MapPath(resources.StyleSheetFolder));
            Directory.CreateDirectory(server.MapPath(resources.ImageFolder));

            FileInfo[] files = null;

            if (regex != null)
            {
                // TODO: Implement
            }
            // Try to glob for files if it's not a regular expression
            else
            {
                var filename = value.Substring(value.LastIndexOf('/') + 1);
                var asp_path = value.Substring(0, value.LastIndexOf('/') + 1);

                var di = new DirectoryInfo(Path.GetDirectoryName(server.MapPath(asp_path)));
                files = di.GetFiles(Path.GetFileName(filename), recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
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
                if (InCacheFolder(resources, relative_filename))
                {
                    continue;
                }

                // Find the path diffrence so we can fix up included resources in fx css
                resources.PathOffset[relative_filename] = GetPathOffset(url.Content(relative_filename.Substring(0, relative_filename.Length - info.Name.Length)), url.Content(resources.ScriptFolder));

                // Save options for later use
                resources.Options[relative_filename] = options;

                // Make group as used if we included a resource from it
                if (resources.GroupsLookup.ContainsKey(relative_filename))
                {
                    resources.GroupsUsed.Add(resources.GroupsLookup[relative_filename]);
                }

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
                        resources.Scripts[depth].Add(relative_filename);
                    }
                }
                else if (relative_filename.EndsWith(".css"))
                    {
                        // Ensure that list exists.
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

        // TODO: Implement
        public static MvcHtmlString Image(this HtmlHelper html, string file)
        {
            return null;
        }

        private static void MinifyFile(string newpath, string oldpath, int timeout, bool isScript = true)
        {
            // Try to write the file 5 times before giving up
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    if (isScript)
                        WriteAllTextExclusive(newpath, Yahoo.Yui.Compressor.JavaScriptCompressor.Compress(File.ReadAllText(oldpath)));
                    else
                    {
                        WriteAllTextExclusive(newpath, Yahoo.Yui.Compressor.CssCompressor.Compress(File.ReadAllText(oldpath)));
                    } 
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(timeout / 5);
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

        public static MvcHtmlString RenderResources(this HtmlHelper html)
        {
            string result = "";

            result += RenderResources(html, "all").ToHtmlString();
            
            var resources = GetResources(html);
            if (resources != null)
            {
                // Render all groups we used
                foreach (var groupname in resources.GroupsUsed)
                {
                    result += RenderResources(html, groupname).ToHtmlString();
                }
            }

            return MvcHtmlString.Create(result);
        }

        // TODO: Implement
        public static MvcHtmlString RenderScriptResources(this HtmlHelper html)
        {
            return RenderResources(html, "all", "script");
        }

        // TODO: Implement
        public static MvcHtmlString RenderStyleSheetResources(this HtmlHelper html)
        {
            return RenderResources(html, "all", "stylesheet");
        }

        public static MvcHtmlString RenderResources(this HtmlHelper html, string groupname, string type = "all")
        {
            var url = new UrlHelper(html.ViewContext.RequestContext);
            var server = html.ViewContext.RequestContext.HttpContext.Server;
            var resources = GetResources(html);
            string result = "";

            if (resources != null)
            {
                // Add resources from groups if they have been used
                if (resources.GroupsUsed.Contains(groupname))
                {
                    foreach (var resource in resources.Groups[groupname])
                    {
                        html.Resource(resource);
                    }
                }
                else if(!resources.Groups.ContainsKey(groupname) && groupname != "all")
                {
                    throw new Exception("Can not find resource group " + groupname);
                }

                // Create ordered lists of scripts and stylesheets.
                IEnumerable<int> scriptKeys = resources.Scripts.Keys.OrderByDescending(k => k).AsEnumerable();
                var _scripts = new List<string>();
                foreach (int key in scriptKeys)
                {
                    foreach (string script in resources.Scripts[key])
                    {
                        // Check if we already rendered this resource
                        if (!resources.Rendered.Contains(script) && (type == "stylesheet" || type == "all"))
                        {
                            // Check if the resource belongs to a group and it's the group currently being rendered 
                            if (resources.GroupsLookup.ContainsKey(script))
                            {
                                if (resources.GroupsLookup[script] != groupname)
                                {
                                    continue;
                                }
                            }
                            // Else the group has to be all to let the resource be rendered
                            else if (groupname != "all")
                            {
                                continue;
                            }

                            _scripts.Add(script);
                            resources.Rendered.Add(script);
                        }
                    }
                }

                IEnumerable<int> styleKeys = resources.Stylesheets.Keys.OrderByDescending(k => k).AsEnumerable();
                var _styles = new List<string>();
                foreach (int key in styleKeys)
                {
                    foreach (string style in resources.Stylesheets[key])
                    {
                        // Check if we already rendered this resource
                        if (!resources.Rendered.Contains(style) && (type == "stylesheet" || type == "all"))
                        {
                            // Check if the resource belongs to a group and it's the group currently being rendered 
                            if (resources.GroupsLookup.ContainsKey(style))
                            {
                                if (resources.GroupsLookup[style] != groupname)
                                {
                                    continue;
                                }
                            }
                            // Else the group has to be all to let the resource be rendered
                            else if(groupname != "all")
                            {
                                continue;
                            }

                            _styles.Add(style);
                            resources.Rendered.Add(style);
                        }
                    }
                }

                // Force bundle and mimify rebuild
                if (resources.Debug)
                {
                    resources.LatestScriptFile = DateTime.Now;
                    resources.LatestCSSFile = DateTime.Now;
                }

                // Minify, if enabled
                if (resources.Minify)
                {
                    if (!resources.Bundle) throw new ConfigurationException("Minify requires bundle to be enabled.");

                    var _styles_min = new List<string>();
                    var _script_min = new List<string>();
                    foreach (var file in _scripts.Union(_styles))
                    {
                        List<string> list_min;
                        int start = (file.LastIndexOf('/') + 1);
                        string origname = file.Substring(start, (file.LastIndexOf('.') - start));
                        FileInfo info = new FileInfo(server.MapPath(file));
                        string newpath = resources.ScriptFolder + origname + ".min" + info.Extension;
                        bool isScript = info.Extension == ".js";

                        if (isScript)    
                            list_min = _script_min;
                        else                            
                            list_min = _styles_min;

                        // The resource is pre-minified. Skip.
                        if (origname.EndsWith(".min"))
                        {
                            list_min.Add(file);
                            continue;
                        }
                        // The resource is marked not to be minified. Skip.
                        if (resources.Options[file].Minify == false)
                        {
                            list_min.Add(file);
                            continue;
                        }
                        // The resource is older than the minified version in cache (if any). Skip.
                        if (!resources.Debug && File.Exists(server.MapPath(newpath)) && DateTime.Compare(File.GetLastWriteTime(server.MapPath(newpath)), info.LastWriteTime) >= 0)
                        {
                            // Update date of latest known Script/CSS if this one is newer.
                            if (isScript && DateTime.Compare(resources.LatestScriptFile, info.LastWriteTime) < 0)
                            {
                                resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(newpath));
                            }
                            else if (!isScript && DateTime.Compare(resources.LatestCSSFile, info.LastWriteTime) < 0)
                            {
                                resources.LatestCSSFile = File.GetLastWriteTime(server.MapPath(newpath));
                            }
                            resources.PathOffset[newpath] = resources.PathOffset[file];
                            resources.Options[newpath] = resources.Options[file];
                            list_min.Add(newpath);
                            continue;
                        }

                        // Minify
                        MinifyFile(server.MapPath(newpath), server.MapPath(file), resources.MimifyTimeout, isScript);

                        if (isScript)
                            resources.LatestScriptFile = File.GetLastWriteTime(server.MapPath(newpath));
                        else
                            resources.LatestCSSFile = File.GetLastWriteTime(server.MapPath(newpath));

                        resources.PathOffset[newpath] = resources.PathOffset[file];
                        resources.Options[newpath] = resources.Options[file];
                        list_min.Add(newpath);
                    }
                    _scripts = _script_min;
                    _styles = _styles_min;
                }

                // If bundle, bundle and add single script/style tags
                if (resources.Bundle)
                {
                    List<string> bScripts = _scripts.Where(s => resources.Options[s].Bundle != false).ToList();
                    _scripts = _scripts.Except(bScripts).ToList();
                    List<string> bStyles = _styles.Where(s => resources.Options[s].Bundle != false).ToList();
                    _styles = _styles.Except(bStyles).ToList();

                    if (bScripts.Count > 0)
                    {
                        // Get a hash of the files in question and generate a path.
                        string scriptPath = resources.ScriptFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", bScripts)))).Replace("-", "").ToLower() + "." + groupname + "-bundle.js";
                        BundleFiles(server, resources.LatestScriptFile, bScripts, resources.PathOffset, scriptPath, resources.Strict, resources.BundleTimeout);
                        result += "<script src=\"" + url.Content(scriptPath) + "?" + String.Format("{0:yyyyMMddHHmmss}", File.GetLastWriteTime(server.MapPath(scriptPath))) + "\" type=\"text/javascript\"></script>\n";
                    }

                    if (bStyles.Count > 0)
                    {
                        // Get a hash of the files in question and generate a path.
                        string cssPath = resources.StyleSheetFolder + BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(string.Join(";", bStyles)))).Replace("-", "").ToLower() + "." + groupname + "-bundle.css";
                        BundleFiles(server, resources.LatestCSSFile, bStyles, resources.PathOffset, cssPath, resources.Strict, resources.BundleTimeout);
                        result += "<link href=\"" + url.Content(cssPath) + "?" + String.Format("{0:yyyyMMddHHmmss}", File.GetLastWriteTime(server.MapPath(cssPath))) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
                    }
                }

                // Render script/style tags
                foreach (string resource in _scripts)
                {
                    DateTime dt = File.GetLastWriteTime(server.MapPath(resource));
                    result += "<script src=\"" + url.Content(resource) + "?" + String.Format("{0:yyyyMMddHHmmss}", dt) + "\" type=\"text/javascript\"></script>\n";
                }
                foreach (string resource in _styles)
                {
                    DateTime dt = File.GetLastWriteTime(server.MapPath(resource));
                    result += "<link href=\"" + url.Content(resource) + "?" + String.Format("{0:yyyyMMddHHmmss}", dt) + "\" rel=\"stylesheet\" type=\"text/css\" />\n";
                }
            }

            return MvcHtmlString.Create(result);
        }

        private static void BundleFiles(HttpServerUtilityBase server, DateTime latest, List<string> files, Dictionary<String, String> offset, string output, bool strict, int timeout)
        {
            var outputfile = server.MapPath(output);

            if (File.Exists(server.MapPath(output)) && DateTime.Compare(File.GetLastWriteTime(outputfile), latest) >= 0)
            {
                // We have already bundled the files.
            }
            else
            {
                var success = false;
                for (int i = 0; i < 5; i++)
                {
                    try
                    {
                        File.Delete(outputfile);
                        var fs = new FileStream(outputfile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                        using (var writer = new StreamWriter(fs, new UTF8Encoding(false)))
                        {
                            foreach (string file in files.ToArray())
                            {
                                if (file.EndsWith(".css"))
                                {
                                    writer.Write("/*" + file + "*/\n");
                                    writer.Write(cssFixup(Path.GetDirectoryName(outputfile), server.MapPath(file), offset[file], strict) + "\n\n");
                                }
                                else
                                {
                                    // TODO: Do something for javascript with fixup
                                    writer.Write("/*" + file + "*/\n");
                                    writer.Write(File.ReadAllText(server.MapPath(file)) + ";\n\n");
                                }
                            }
                        }
                        success = true;
                        break;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(timeout / 5);
                    }
                }
                if (!success)
                {
                    throw new Exception("Could not write " + outputfile + " within a timeout of " + timeout + "ms");
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

        private static void WriteAllTextExclusive(string path, string text)
        {
            var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using (StreamWriter writer = new StreamWriter(fs, new UTF8Encoding(false)))
            {
                writer.Write(text);
            }
        }

        private static HtmlResources GetResources(HtmlHelper html)
        {
            // Store state in the ViewData //TODO: Implment some kind of IIS caching so we don't do this for every page load
            var resources = (HtmlResources)html.ViewContext.HttpContext.Items["Resources"];

            if (resources == null)
            {
                resources = new HtmlResources();
                html.ViewContext.HttpContext.Items["Resources"] = resources;
            }
            return resources;
        }
    }
}
