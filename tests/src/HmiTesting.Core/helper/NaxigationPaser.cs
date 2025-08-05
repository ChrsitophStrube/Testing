using System.Xml.Linq;
using static HmiTesting.Core.Helpers.PathHandler;

namespace HmiTesting.Core.Helpers
{
    public static class NaxigationPaser
    {
        private static readonly XNamespace Ns = "MyHMI.NavigationContent";

        public static Dictionary<OpcPath, string> GetScreensFromNavigationXML(string xmlFile)
        {
            var doc = XDocument.Load(xmlFile);

            var result = new Dictionary<OpcPath, string>();

            void Walk(XElement elt, List<string> titles)
            {
                var title = (string?)elt.Attribute("Title") ?? string.Empty;
                var screenPath = ((string?)elt.Attribute("ScreenPath"))?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(title))
                    titles.Add(title.Trim());

                if (!string.IsNullOrWhiteSpace(screenPath))
                {
                    var screenName = GetLastSegment(screenPath);
                    var key = BuildPath(titles.ToArray());

                    if (!result.ContainsKey(key))
                        result.Add(key, screenName);
                }

                foreach (var child in elt.Elements(Ns + "NavItem"))
                    Walk(child, new List<string>(titles));
            }

            foreach (var root in doc.Root!.Elements(Ns + "NavItem"))
                Walk(root, new List<string>());

            return result;
        }

        private static string GetLastSegment(string screenPath)
        {
            var idx = screenPath.LastIndexOf('/');
            return (idx >= 0 ? screenPath[(idx + 1)..] : screenPath).Trim();
        }
    }
}
