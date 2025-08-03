using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using NUnit.Framework.Constraints;
public class CMPDropdown : CMPInput
{
    protected IOpcUaSession _session;
    public LocatorNodeId _dropdown { get; }
    public CMPDropdown(IOpcUaSession session, LocatorNodeId dropdown) : base(session, dropdown)
    {
        _session = session;
        _dropdown = dropdown;
    }


    public async Task SelectOption(string optionText)
    {
        //Open the dropdown
        var button = _session.GetNodeLocator(_dropdown.Locator.Page, "Button", _dropdown.NodeId);
        await button.Locator.ClickAsync();

        //Get all Options
        var verticalLayout = _session.GetNodeIdFromPath(BuildPath("CoT_CMP_DropdownContent", "ScrollView", "VerticalLayout").ToString(), button.NodeId);
        Thread.Sleep(500); //wait until the dropdown is open(Flyout visible)
        List<NodeId> options = _session.GetChildren(verticalLayout);

        //Get the option with the specified text
        foreach (var optionId in options)
        {
            // Text des Eintrags lesen
            NodeId optionTextId = _session.GetNodeIdFromPath(BuildPath("Label", "text").ToString(), optionId);
            LocalizedText optionTextValue = _session.GetValue<LocalizedText>(optionTextId);

            // Option ID Number Reading
            NodeId selectionOptionPointer = _session.GetNodeIdFromPath("selectionOption", optionId);
            NodeId selectionOptionNodeId = _session.GetValue<NodeId>(selectionOptionPointer);
            NodeId selectionOptionId = _session.GetNodeIdFromPath("id", selectionOptionNodeId);
            int selectedOptionObject = _session.GetValue<int>(selectionOptionId);

            _session.GetValue<int>(selectionOptionId);
            if (optionTextValue.Text == optionText)
            {
                // Click the row with the matching text
                var optionLocator = _session.GetNodeLocator(
                    _dropdown.Locator.Page, "Button", optionId);
                await optionLocator.Locator.ClickAsync();

                NodeId selectedId = _session.GetNodeIdFromPath("selectedID", _dropdown.NodeId);
                int selectedOptionDropdown = _session.GetValue<int>(selectedId);
                Assert.AreEqual(selectedOptionDropdown, selectedOptionObject,
                    $"Selected option ID {selectedOptionDropdown} does not match expected ID {selectedOptionObject} for option '{optionText}'.");
                return;
            }
        }

        throw new InvalidOperationException(
            $"Option '{optionText}' was  not found.");
    }


    public int GeSelectedId()
    {   //get OPC variable
        NodeId selectedId = _session.GetNodeIdFromPath("selectedID", _dropdown.NodeId);
        return _session.GetValue<int>(selectedId);
    }

    public async Task<string> SelectedText()
    {
        //get string from Dom
        OpcPath textPath = BuildPath("HorizontalLayout", "Label", "HorizontalLayout", "Text");
        LocatorNodeId selectdeOptionText = _session.GetNodeLocator(_dropdown.Locator.Page, textPath.ToString(), _dropdown.NodeId);
        return await selectdeOptionText.Locator.Locator("span").InnerTextAsync();
    }

    public async Task<bool> IsEnabled()
    {
        // Check if button forwards events
        var button = _session.GetNodeLocator(_dropdown.Locator.Page, "Button", _dropdown.NodeId);
        string pointerEvents = await button.Locator.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).pointerEvents"
        );
        bool eventsActive = pointerEvents == "auto";

        //Chech if Dropdown arrow  Icon state is disabled
        var svgId = _session.GetNodeLocator(_dropdown.Locator.Page, BuildPath("HorizontalLayout", "DropdownButton").ToString(), _dropdown.NodeId);
        SvgState svgState = new("dropdown", null, eventsActive ? "enabled" : "disabled");
        await MatchSvgStateAsync(svgId.Locator, svgState);

        return eventsActive;
    }
}