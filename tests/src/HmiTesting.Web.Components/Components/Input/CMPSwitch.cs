using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Text;
using System.Text.RegularExpressions;
public class CMPSwitch : CMPInput
{
    protected IOpcUaSession _session;
    public LocatorNodeId _switch { get; }
    public CMPSwitch(IOpcUaSession session, LocatorNodeId @switch) : base(session, @switch)
    {
        _session = session;
        _switch = @switch;
    }

    public async Task<bool> GetCommand()
    {
        //Check Ouput Variable State of the Switch
        NodeId commandOutputId = _session.GetNodeIdFromPath("command", _switch.NodeId);
        bool commandOutput = _session.GetValue<bool>(commandOutputId);


        //Check Visual State of the Switch
        var svgId = _session.GetNodeLocator(_switch.Locator.Page, BuildPath("CoT_LedSwitch", "Icon").ToString(), _switch.NodeId);
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

        var switchButton = _session.GetNodeLocator(_switch.Locator.Page, "SwitchButton", _switch.NodeId);
        await switchButton.Locator.ClickAsync();

        switchState = await GetCommand();
        if (switchState != command)
        {
            throw new Exception("the command output does not match the visual state after setting the command");
        }
    }
    public async Task<bool> IsEnabled()
    {
        // Check if button forwards events
        var button = _session.GetNodeLocator(_switch.Locator.Page, "SwitchButton", _switch.NodeId);
        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        bool eventsActive = pointerEvents == "auto";

        //Chech if Icon state is disabled
        var svgId = _session.GetNodeLocator(_switch.Locator.Page, BuildPath("CoT_LedSwitch", "Icon").ToString(), _switch.NodeId);
        SvgState svgState = new("led", null, eventsActive ? "enabled" : "disabled");
        await MatchSvgStateAsync(svgId.Locator, svgState);

        return eventsActive;
    }

}