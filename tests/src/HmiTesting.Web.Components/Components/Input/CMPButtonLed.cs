using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;
using System.Text;
using System.Text.RegularExpressions;
using static HmiTesting.Core.Helpers.PathHandler;
public class CMPButtonLed : CMPButtonAction
{
    public CMPButtonLed(IOpcUaSession session, LocatorNodeId button) : base(session, button)
    {
    }
    public async Task<string> GetLedState()
    {
        var ledSvgId = _session.GetNodeLocator(_button.Locator.Page, BuildPath("HorizontalLayout1", "CoT_LedMulticolor1").ToString(), _button.NodeId);
        string dataUri = await ledSvgId.Locator.Locator("img").GetAttributeAsync("src");

        const string prefix = "data:image/svg+xml;base64,";


        // Base-64 Decode
        string base64 = dataUri[prefix.Length..];
        byte[] bytes = Convert.FromBase64String(base64);
        string svgText = Encoding.UTF8.GetString(bytes);      // UTF-8

        // data-testid per Regex herausfischen
        var m = Regex.Match(svgText,
                            @"data-testid\s*=\s*[""']([^""']+)[""']",
                            RegexOptions.IgnoreCase);

        return m.Success ? m.Groups[1].Value : null;
    }
}
