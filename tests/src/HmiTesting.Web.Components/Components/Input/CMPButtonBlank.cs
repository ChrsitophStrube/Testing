using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
public class CMPButtonBlank : IInputControl
{
    protected IOpcUaSession _session;
    public LocatorNodeId _button { get; }
    public CMPButtonBlank(IOpcUaSession session, LocatorNodeId button)
    {
        _session = session;
        _button = button;
    }

    public async Task Click()
    {
        var button = _session.ResolveNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);
        await button.Locator.ClickAsync();
    }

    public async Task LongClick(int durationMs)
    {
        var button = _session.ResolveNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);
        await button.Locator.ClickAsync(new LocatorClickOptions { Delay = durationMs });
    }

    public async Task<string> GetIconName()
    {
        LocatorNodeId IconElement = _session.ResolveNodeLocator(_button.Locator.Page, BuildPath("HorizontalLayout1", "Icon").ToString(), _button.NodeId);
        int IconExists = await IconElement.Locator.CountAsync();
        if (IconExists == 0)
        {
            return string.Empty; // No icon present
        }
        else
        {
            LocatorNodeId iconpathLocator = _session.ResolveNodeLocator(_button.Locator.Page, "icon", _button.NodeId);
            string iconpathstring = _session.GetValue<string>(iconpathLocator.NodeId);
            return Path.GetFileName(iconpathstring); // Return the icon file name
        }
    }

    public string GetUserRole()
    {
        LocatorNodeId iconpathLocator = _session.ResolveNodeLocator(_button.Locator.Page, "userRole", _button.NodeId);
        NodeId userRoleId = _session.GetValue<NodeId>(iconpathLocator.NodeId);
        return _session.GetBrowsename(userRoleId);
    }

    public async Task<bool> IsEnabled()
    {
        var button = _session.ResolveNodeLocator(_button.Locator.Page, "TransparentButton", _button.NodeId);
        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        return pointerEvents == "auto";
    }

    public async Task<bool> IsVisibleAsync(int timeoutMs = 2000)
    {
        NodeId visebiletyId = _session.GetNodeIdFromPath("visibility", _button.NodeId);
        bool visebilety = _session.GetValue<bool>(visebiletyId);
        WaitForSelectorState state = visebilety ? WaitForSelectorState.Visible : WaitForSelectorState.Detached;
        await _button.Locator.WaitForAsync(new()
        {
            State = state,
            Timeout = timeoutMs
        });
        return visebilety;
    }
}