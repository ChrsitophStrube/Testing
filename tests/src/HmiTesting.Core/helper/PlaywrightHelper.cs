using System;
using System.IO;
using System.Threading.Tasks;
using LibUA.Core;
using Microsoft.Playwright;
using NUnit.Framework;
using LibUA;
using System.Text;
using System.Text.RegularExpressions;
using HmiTesting.Core.DTOs;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using HmiTesting.Core.Interfaces;
using NUnit.Framework.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Xml.Linq;


namespace HmiTesting.Core.Helpers
{
    public static class PlaywrightHelper
    {


        public static async Task MakePageScreenshot(IHmiPage hmiPage, string pageName, IOpcUaSession session)
        {
            var page = hmiPage.Page;

            var root = ScreenshotOnFailureAttribute.FindRepoRoot(TestContext.CurrentContext.WorkDirectory);

            var folder = Path.Combine(root, "test-results", "screenshots", "General");
            Directory.CreateDirectory(folder);

            var safeName = ScreenshotOnFailureAttribute.Sanitize(pageName);
            var file = Path.Combine(folder, $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmssfff}.png");
            Thread.Sleep(200);
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
            TestContext.AddTestAttachment(file, $"Screenshot: {pageName}");
        }


        //State of an  SVG Image
        public readonly record struct SvgState(string Item, string State, string Mode);

        /// <summary>
        /// Extracts Type and State and mode from a SVG Image.
        /// </summary>
        public static SvgState ParseSvgState(string dataUri)
        {
            const string prefix = "data:image/svg+xml;base64,";

            if (string.IsNullOrWhiteSpace(dataUri) ||
                !dataUri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Unexpected data URI format.", nameof(dataUri));
            }

            // 1) Base-64 dekodieren
            string base64 = dataUri[prefix.Length..];
            byte[] bytes = Convert.FromBase64String(base64);
            string svgText = Encoding.UTF8.GetString(bytes);

            // 2) Regex: item=…,state=…,mode=…
            //    Beispiel: data-testid="item=checkbox,state=checked,mode=disabled"
            Match m = Regex.Match(
                svgText,
                @"data-testid\s*=\s*[""']item=(?<item>[^,]+),state=(?<state>[^,]+),mode=(?<mode>[^""']+)[""']",
                RegexOptions.IgnoreCase);

            if (!m.Success)
                throw new FormatException("Could not find data-testid with item/state/mode in SVG.");

            return new SvgState(
                m.Groups["item"].Value,
                m.Groups["state"].Value,
                m.Groups["mode"].Value);
        }



        public static async Task<bool> MatchSvgStateAsync(
        ILocator locator,            // z. B. ledSvgId
        SvgState expected,
        int timeoutMs = 1000,
        int pollMs = 50)
        {
            var img = locator.Locator("img");
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                // <img src="...">
                string? src = await img.GetAttributeAsync("src");
                if (src is { Length: > 0 } && src.StartsWith("data:image"))
                {
                    // 1) Base-64 Teil nach dem Komma holen
                    string b64 = src[(src.IndexOf(',') + 1)..];

                    // 2) Dekodieren (fehlerhafte Strings überspringen)
                    if (Convert.TryFromBase64String(b64, new Span<byte>(new byte[b64.Length]), out int len))
                    {
                        string svg = Encoding.UTF8.GetString(
                                         Convert.FromBase64String(b64));

                        // 3) data-testid suchen & parsen
                        SvgState actual = ParseSvgState(src);
                        if (SvgMatches(actual, expected))
                            return true;
                    }
                }

                await Task.Delay(pollMs);
            }
            throw new TimeoutException(
                $"SVG state did not match within {timeoutMs} ms: " +
                $"Expected: {expected}, Actual: {await img.GetAttributeAsync("src")}");
        }

        static bool SvgMatches(SvgState a, SvgState e)
        {
            return (e.Item == null || a.Item == e.Item)
            && (e.State == null || a.State == e.State)
            && (e.Mode == null || a.Mode == e.Mode);
        }



        private static readonly Regex RgbRegex = new(
            //  r     g     b       optional Alpha (0-1 oder 0-255)
            @"rgba?\s*\(\s*(?<r>\d{1,3})\s*[,\s]\s*
                   (?<g>\d{1,3})\s*[,\s]\s*
                   (?<b>\d{1,3})
                   (?:\s*[,\s/]\s*(?<a>[0-9.]+))?
               \s*\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        public static Color ParseCssToColor(string css)
        {
            if (string.IsNullOrWhiteSpace(css))
                throw new ArgumentException("CSS color string is null or empty.", nameof(css));

            var m = RgbRegex.Match(css);
            if (!m.Success)
                throw new FormatException($"Can't parse color '{css}'");

            // Grundwerte
            byte r = byte.Parse(m.Groups["r"].Value, CultureInfo.InvariantCulture);
            byte g = byte.Parse(m.Groups["g"].Value, CultureInfo.InvariantCulture);
            byte b = byte.Parse(m.Groups["b"].Value, CultureInfo.InvariantCulture);

            // Alpha calculation (missing → 255)
            byte a = 255;
            if (m.Groups["a"].Success)
            {
                string aStr = m.Groups["a"].Value;
                if (aStr.Contains('.'))
                {
                    double af = double.Parse(aStr, CultureInfo.InvariantCulture);
                    a = (byte)Math.Round(af * 255);
                }
                else
                {
                    a = byte.Parse(aStr, CultureInfo.InvariantCulture);
                }
            }

            return Color.FromArgb(a, r, g, b);
        }


        public static string ParseColorToCss(Color c)
        {
            // if alpha = 255 ⇒ "rgb(...)"
            if (c.A == 255)
                return $"rgb({c.R}, {c.G}, {c.B})";

            double a = c.A / 255d;
            return string.Create(
                CultureInfo.InvariantCulture,
                $"rgba({c.R}, {c.G}, {c.B}, {a:0.###})");
        }


        //Extention method to compare colors (Hex optix <-> HTML CSS)
        public static bool EqualsRgba(this Color self, Color other) =>
            self.R == other.R && self.G == other.G &&
            self.B == other.B && self.A == other.A;


        public static async Task WaitForCssColorAsync(
        ILocator locator,
        string cssProperty,
        params Color[] expectedColors)
        {
            if (expectedColors == null || expectedColors.Length == 0)
                throw new ArgumentException("At least one expected color must be provided.", nameof(expectedColors));


            string pattern = $"^(?:{string.Join("|",
                expectedColors.Select(c => Regex.Escape(ParseColorToCss(c))))}?)$";

            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            try
            {
                await Assertions.Expect(locator)
                                .ToHaveCSSAsync(cssProperty, regex);
            }
            catch (PlaywrightException ex)
            {

                string cssActual = await locator.EvaluateAsync<string>(
                    $"el => window.getComputedStyle(el).getPropertyValue('{cssProperty}')");

                Color actualColour = ParseCssToColor(cssActual);
                string actualHex = actualColour.ToArgb().ToString("X8");

                string expectedHex = string.Join(" | ",
                    expectedColors.Select(c => c.ToArgb().ToString("X8")));

                throw new PlaywrightException(
                    $"Colour mismatch for CSS property '{cssProperty}'.\n" +
                    $"Expected : {expectedHex}\n" +
                    $"Actual   : {actualHex}",
                    ex);
            }
        }



        public static async Task<Color> GetCssColorAsync(ILocator locator, string cssProperty)
        {

            string color = await locator.EvaluateAsync<string>(
                $"el => window.getComputedStyle(el).getPropertyValue('{cssProperty}')");

            return ParseCssToColor(color);
        }
    }
}




