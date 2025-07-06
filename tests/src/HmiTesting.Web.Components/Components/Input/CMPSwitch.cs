using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Text;
using System.Text.RegularExpressions;
public class CMPSwitch : IInputControl
{
    protected IOpcUaSession _session;
    public LocatorNodeId _switch { get; }
    public CMPSwitch(IOpcUaSession session, LocatorNodeId @switch)
    {
        _session = session;
        _switch = @switch;
    }

    public async Task Click()
    {
        var button = _session.ResolveNodeLocator(_switch.Locator.Page, "TransparentButton", _switch.NodeId);
        await button.Locator.ClickAsync();
    }

    public async Task<bool> GetCommand()
    {
        //Check Ouput Variable State of the Switch
        NodeId commandOutputId = _session.GetNodeIdFromPath("command", _switch.NodeId);
        bool commandOutput = _session.GetValue<bool>(commandOutputId);


        //Check Visual State of the Switch
        var svgId = _session.ResolveNodeLocator(_switch.Locator.Page, BuildPath("CoT_LedSwitch", "Icon").ToString(), _switch.NodeId);
        SvgState svgState = new("led", commandOutput ? "on-finished" : "off", null);
        await MatchSvgStateAsync(svgId.Locator, svgState);

        return commandOutput;
    }

    public async Task SetCommand(bool command)
    {
        //Check Visual State of the Switch
        bool switchState = await GetCommand();
        if (switchState == command)
        {
            return; // No change needed
        }

        var switchButton = _session.ResolveNodeLocator(_switch.Locator.Page, "SwitchButton", _switch.NodeId);
        await switchButton.Locator.ClickAsync();

        switchState = await GetCommand();
        if (switchState != command)
        {
            throw new Exception("the command output does not match the visual state after setting the command");
        }
    }




    public string GetUserRole()
    {
        LocatorNodeId iconpathLocator = _session.ResolveNodeLocator(_switch.Locator.Page, "userRole", _switch.NodeId);
        NodeId userRoleId = _session.GetValue<NodeId>(iconpathLocator.NodeId);
        return _session.GetBrowsename(userRoleId);
    }

    public async Task<bool> IsEnabled()
    {
        // Check if button forwards events
        var button = _session.ResolveNodeLocator(_switch.Locator.Page, "SwitchButton", _switch.NodeId);
        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        bool eventsActive = pointerEvents == "auto";

        //Chech if Icon state is disabled
        var svgId = _session.ResolveNodeLocator(_switch.Locator.Page, BuildPath("CoT_LedSwitch", "Icon").ToString(), _switch.NodeId);
        SvgState svgState = new("led", null, eventsActive ? "enabled" : "disabled");
        await MatchSvgStateAsync(svgId.Locator, svgState);

        return eventsActive;
    }

    public async Task<bool> IsVisibleAsync(int timeoutMs = 2000)
    {
        NodeId visebiletyId = _session.GetNodeIdFromPath("visibility", _switch.NodeId);
        bool visebilety = _session.GetValue<bool>(visebiletyId);
        WaitForSelectorState state = visebilety ? WaitForSelectorState.Visible : WaitForSelectorState.Detached;
        await _switch.Locator.WaitForAsync(new()
        {
            State = state,
            Timeout = timeoutMs
        });
        return visebilety;
    }

}