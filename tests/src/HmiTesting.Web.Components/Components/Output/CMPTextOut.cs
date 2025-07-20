using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
public class CMPTextOut : CMPOutput 
{
    private IOpcUaSession _session;
    public LocatorNodeId _textOut { get; }
    public CMPTextOut(IOpcUaSession session, LocatorNodeId textOut) : base(session, textOut)
    {
        _session = session;
        _textOut = textOut;
    }

    public string UnitProperty { get => GetProperty<string>("unit"); set => SetProperty("unit", value); }
    public async Task WaitForUnitProperty(string unit) => await WaitForProperty<string>("unit", unit);

    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public async Task WaitForErrorStateProperty(bool errorstate) => await WaitForProperty<bool>("errorState", errorstate);

    public LocalizedText TextProperty { get => GetProperty<LocalizedText>("text"); set => SetProperty("text", value); }
    public async Task WaitForTextProperty(LocalizedText text) => await WaitForProperty<LocalizedText>("text", text);

    public async Task<string> GetText()
    {
        var spinBox = _session.ResolveNodeLocator(_textOut.Locator.Page, BuildPath("VerticalLayout", "TextDisplay").ToString(), _textOut.NodeId);
        return await spinBox.Locator.Locator("span").InnerTextAsync();
    }

    public async Task WaitForText(string value)
    {
        var spinBox = _session.ResolveNodeLocator(_textOut.Locator.Page, BuildPath( "VerticalLayout", "TextDisplay").ToString(), _textOut.NodeId);
        var expected = value;
        await Assertions.Expect(spinBox.Locator.Locator("input")).ToHaveValueAsync(expected);
    }

    public async Task<bool> GetErrorState()
    {

        // get TextColor
        LocatorNodeId label = _session.ResolveNodeLocator(_textOut.Locator.Page, BuildPath("VerticalLayout", "TextDisplay").ToString(), _textOut.NodeId);
        ILocator textColorLocator = label.Locator.Locator("span").First;
        Color textColor = await GetCssColorAsync(textColorLocator, "color");

        bool textColorErrorState;
        if (textColor.EqualsRgba(fatal100))
        {
            textColorErrorState = true;
        }
        else if (textColor.EqualsRgba(dark100))
        {
            textColorErrorState = false;
            return false; // White text color indicates no error 
        }
        else
        {
            throw new Exception($"Unexpected text color: {textColor}");
        }

        // get Color of BottomLine
        LocatorNodeId bottomLine = _session.ResolveNodeLocator(_textOut.Locator.Page, "BottomLine", _textOut.NodeId);
        ILocator bottomLineLocator = bottomLine.Locator.Locator("div");
        Color backgroundColor = await GetCssColorAsync(bottomLineLocator, "background-color");

        bool backgroundColorErrorState;
        if (backgroundColor.EqualsRgba(fatal100)) // Red background indicates error
        {
            backgroundColorErrorState = true;
        }
        else
        {
            throw new Exception($"Unexpected background color: {backgroundColor}");
        }

        if (textColorErrorState == backgroundColorErrorState)
        {
            return textColorErrorState;
        }
        throw new Exception($"Error state missmatch. Background has color: {backgroundColor} Text has color{textColor}");
    }

    public async Task WaitForErrorState(bool errorstate)
    {
        // get Locator for TextColor
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_textOut.Locator.Page, BuildPath("VerticalLayout", "TextDisplay").ToString(), _textOut.NodeId);
        ILocator textColorLocator = spinBox.Locator.Locator("span").First;

        // get Locator for BackgroundColor
        LocatorNodeId bottomLine = _session.ResolveNodeLocator(_textOut.Locator.Page, "BottomLine", _textOut.NodeId);
        ILocator bottomLineLocator = bottomLine.Locator.Locator("div");

        if (errorstate)
        {
            // Wait for error state
            await WaitForCssColorAsync(bottomLineLocator, "background-color", fatal100);
            await WaitForCssColorAsync(textColorLocator, "color", fatal100);
        }
        else
        {
            // Wait for no error state
            await WaitForCssColorAsync(textColorLocator, "color",dark100);
        }
    }



}