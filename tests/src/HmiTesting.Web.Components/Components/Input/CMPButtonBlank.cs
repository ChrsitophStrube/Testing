using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
public class CMPButtonBlank : CMPInput
{
    protected IOpcUaSession _session;
    public LocatorNodeId _button { get; }
    public CMPButtonBlank(IOpcUaSession session, LocatorNodeId button) : base(session, button)
    {
        _session = session;
        _button = button;
    }

    public async Task Click()
    {
        var button = _session.GetNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);
        await button.Locator.ClickAsync();
    }

    public async Task LongClick(int durationMs)
    {
        var button = _session.GetNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);
        await button.Locator.ClickAsync(new LocatorClickOptions { Delay = durationMs });
    }

    public async Task<string> GetIconName()
    {
        LocatorNodeId IconElement = _session.GetNodeLocator(_button.Locator.Page, BuildPath("HorizontalLayout1", "Icon").ToString(), _button.NodeId);
        int IconExists = await IconElement.Locator.CountAsync();
        if (IconExists == 0)
        {
            return string.Empty; // No icon present
        }
        else
        {
            LocatorNodeId iconpathLocator = _session.GetNodeLocator(_button.Locator.Page, "icon", _button.NodeId);
            string iconpathstring = _session.GetValue<string>(iconpathLocator.NodeId);
            return Path.GetFileName(iconpathstring); // Return the icon file name
        }
    }

    public async Task<bool> IsEnabled()
    {
        var button = _session.WaitForNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);

        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        return pointerEvents == "auto";
    }
    
    public async Task WaitForEnabled(bool enabled)
    {
        string expected = enabled ? "auto" : "none";
        var button = _session.WaitForNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);
        
        await Assertions.Expect(button.Locator)
            .ToHaveCSSAsync("pointer-events", expected,
                new() { Timeout = 2000 });
    }

}