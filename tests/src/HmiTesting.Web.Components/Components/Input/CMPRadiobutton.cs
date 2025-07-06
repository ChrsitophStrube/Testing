using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;

public class CMPRadiobutton : IInputControl
{
    protected IOpcUaSession _session;
    public LocatorNodeId _radiobutton { get; }
    public CMPRadiobutton(IOpcUaSession session, LocatorNodeId radiobutton)
    {
        _session = session;
        _radiobutton = radiobutton;
    }

    public async Task<bool> GetChecked()
    {
        //Check Ouput Variable State of the Selected Option of the RadioButton
        NodeId selectedOptionId = _session.GetNodeIdFromPath("selectedOption", _radiobutton.NodeId);
        Int16 selectedOption = _session.GetValue<Int16>(selectedOptionId);

        //Check Ouput Variable State of the OptionId of the RadioButton
        NodeId optionIdId = _session.GetNodeIdFromPath("optionID", _radiobutton.NodeId);
        Int16 optionId = _session.GetValue<Int16>(optionIdId);
        //Determine if the selected option matches the option ID
        bool checkedOutput = selectedOption == optionId;

        //Check Visual State of the Switch
        bool visualState = false;
        var svgId = _session.ResolveNodeLocator(_radiobutton.Locator.Page, "Icon", _radiobutton.NodeId);

        string dataUri = await svgId.Locator.Locator("img").GetAttributeAsync("src");
        SvgState svgState = new("radiobutton", checkedOutput ? "checked" : "unchecked", null);
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

        var switchButton = _session.ResolveNodeLocator(_radiobutton.Locator.Page, "TransparentButton", _radiobutton.NodeId);
        await switchButton.Locator.ClickAsync();

        switchState = await GetChecked();
        if (switchState != command)
        {
            throw new Exception("the command output does not match the visual state after setting the Checked State");
        }
    }


    public string GetUserRole()
    {
        LocatorNodeId iconpathLocator = _session.ResolveNodeLocator(_radiobutton.Locator.Page, "userRole", _radiobutton.NodeId);
        NodeId userRoleId = _session.GetValue<NodeId>(iconpathLocator.NodeId);
        return _session.GetBrowsename(userRoleId);
    }

    public async Task<bool> IsEnabled()
    {
        // Check if button forwards events
        var button = _session.ResolveNodeLocator(_radiobutton.Locator.Page, "TransparentButton", _radiobutton.NodeId);
        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        bool eventsActive = pointerEvents == "auto";
        //Check svg state 
        var svgId = _session.ResolveNodeLocator(_radiobutton.Locator.Page, "Icon", _radiobutton.NodeId);
        SvgState svgState = new("radiobutton", null, eventsActive ? "enabled" : "disabled");
        await MatchSvgStateAsync(svgId.Locator, svgState);
        return eventsActive;
    }

    public async Task<bool> IsVisibleAsync(int timeoutMs = 2000)
    {
        NodeId visebiletyId = _session.GetNodeIdFromPath("visibility", _radiobutton.NodeId);
        bool visebilety = _session.GetValue<bool>(visebiletyId);
        WaitForSelectorState state = visebilety ? WaitForSelectorState.Visible : WaitForSelectorState.Detached;
        await _radiobutton.Locator.WaitForAsync(new()
        {
            State = state,
            Timeout = timeoutMs
        });
        return visebilety;
    }
}