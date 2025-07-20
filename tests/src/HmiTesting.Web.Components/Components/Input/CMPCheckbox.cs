using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Text;
using System.Text.RegularExpressions;
public class CMPCheckbox : CMPInput
{
    protected IOpcUaSession _session;
    public LocatorNodeId _checkbox { get; }
    public CMPCheckbox(IOpcUaSession session, LocatorNodeId checkBox) : base(session, checkBox)
    {
        _session = session;
        _checkbox = checkBox;
    }

    public async Task<bool> GetChecked()
    {
        //Check Ouput Variable State of the Switch
        NodeId checkedOutputId = _session.GetNodeIdFromPath("checked", _checkbox.NodeId);
        bool checkedOutput = _session.GetValue<bool>(checkedOutputId);

        //Check Visual State of the Switch
        bool visualState = false;
        var svgId = _session.ResolveNodeLocator(_checkbox.Locator.Page, "Icon", _checkbox.NodeId);

        string dataUri = await svgId.Locator.Locator("img").GetAttributeAsync("src");
        SvgState svgState = new("checkbox", checkedOutput ? "checked" : "unchecked", null);
        await MatchSvgStateAsync(svgId.Locator, svgState);
        return checkedOutput;
    }

    public async Task SetChecked(bool command)
    {
        //Check Visual State of the Switch
        bool switchState = await GetChecked();
        if (switchState == command)
        {
            return; // No change needed
        }

        var switchButton = _session.ResolveNodeLocator(_checkbox.Locator.Page, "TransparentButton", _checkbox.NodeId);
        await switchButton.Locator.ClickAsync();

        switchState = await GetChecked();
        if (switchState != command)
        {
            throw new Exception("the command output does not match the visual state after setting the Checked State");
        }
    }

    public async Task<bool> IsEnabled()
    {
        // Check if button forwards events
        var button = _session.ResolveNodeLocator(_checkbox.Locator.Page, "TransparentButton", _checkbox.NodeId);
        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        bool eventsActive = pointerEvents == "auto";

        //Check svg state 
        var svgId = _session.ResolveNodeLocator(_checkbox.Locator.Page, "Icon", _checkbox.NodeId);
        SvgState svgState = new("checkbox", null, eventsActive ? "enabled" : "disabled");
        await MatchSvgStateAsync(svgId.Locator, svgState);
        return eventsActive;
    }

}