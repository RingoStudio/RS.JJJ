using RS.Tools.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RS.Snail.JJJ.test
{
    internal class AutoScripts
    {
        public static void FixConditions(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;

            var dir = new System.IO.DirectoryInfo(path);

            foreach (var fileInfo in dir.GetFiles())
            {
                if (fileInfo.Extension.ToLower() != ".cs") continue;
                if (fileInfo.Name.Contains("ICondition")) continue;

                FixCondiitonFile(fileInfo.FullName);
            }
        }

        private static void FixCondiitonFile(string fullName)
        {
            if (!File.Exists(fullName)) return;
            var text = File.ReadAllText(fullName);

            var lines = new List<string>();
            var tag = fullName.Split("\\").Last().Split(".").First();

            var foundMark = false;
            var containsBoot = false;

            foreach (var item in text.Split("\r\n"))
            {
                var line = item ?? "";
                if (line.Contains("public boot.Context _context"))
                {
                    foundMark = true;
                    continue;
                }

                if (line.Contains("this._context = context;"))
                {
                    continue;
                }

                if (foundMark)
                {
                    // New
                    if (line.Contains($"public {tag}(")) line = $"        public {tag}()";
                    // Check
                    else if (line.Contains("public bool Check(")) line = $"        public bool Check(Context context, {line.Split("(").Last()}";
                    // Desc
                    else if (line.Contains("public string Desc(")) line = $"        public string Desc(Context context, {line.Split("(").Last()}";
                    // GetProgress
                    else if (line.Contains("public dynamic GetProgress(")) line = $"        public dynamic GetProgress(Context context, {line.Split("(").Last()}";
                    // Parse
                    else if (line.Contains("public dynamic Parse(")) line = $"        public dynamic Parse(Context context, {line.Split("(").Last()}";
                }

                if (!containsBoot)
                {
                    if (line.StartsWith("using RS.Snail.JJJ.Client.core.boot;"))
                    {
                        containsBoot = true;
                    }
                }

                lines.Add(line);
            }

            if (!foundMark) return;
            if (!containsBoot) lines.Insert(0, "using RS.Snail.JJJ.Client.core.boot;");

            var ret = string.Join("\r\n", lines);
            ret = ret.Replace("Parse(args)", "Parse(context, args)");
            ret = ret.Replace("_context", "context");
            ret = ret.Replace("Check(args, null)", "Check(context, args, null)");

            IOHelper.WriteFile(ret, fullName, true);
        }
    }
}
