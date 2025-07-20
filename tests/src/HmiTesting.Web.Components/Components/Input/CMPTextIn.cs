using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Numerics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Drawing;
using System.Text.RegularExpressions;

public class CMPTextIn : CMPInput
{
    public IOpcUaSession _session;
    public LocatorNodeId _textIn { get; }



    private string _keyboardName;
    private string _actualLocale;



    public CMPTextIn(IOpcUaSession session, LocatorNodeId textIn) : base(session, textIn)
    {
        _session = session;
        _textIn = textIn;


        NodeId actualLocaleID = _session.GetNodeIdFromPath("urn:FTOptix:Core", "ActualLocaleId", _session.GetLastSession());
        _actualLocale = _session.GetValue<string>(actualLocaleID);
        _actualLocale = _actualLocale.Replace("-", "_");
        _keyboardName = "CoT_AlphaNumeric_" + _actualLocale;
    }

    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public Task WaitForErrorStatePropertyAsync(bool errorState) => WaitForProperty<bool>("errorState", errorState);


    public async Task SetValueByKeyboard(string value)
    {
        //OpenKeyboard
        var textInput = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        await textInput.Locator.ClickAsync();

        //getKeyboard
        LocatorNodeId keyboard = _session.ResolveNodeLocator(_textIn.Locator.Page, _keyboardName, _session.GetLastSession());

        //get Preedit Spinbox
        LocatorNodeId preeditTextBox = _session.ResolveNodeLocator(_textIn.Locator.Page, "PreeditTextInput", keyboard.NodeId);

        //Set Value
        await preeditTextBox.Locator.Locator("input").PressAsync("Delete");
        await preeditTextBox.Locator.Locator("input").FillAsync(value);

        //Close Keyboard by clicking enter
        LocatorNodeId enterButton = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("KeyboardLoader_Preedit", "Letters_" + _actualLocale, "KeysRows", "KeysRow4", "Enter").ToString(), keyboard.NodeId);
        await enterButton.Locator.ClickAsync();
    }

    public async Task<string> GetInput()
    {
        var textInput = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        return await textInput.Locator.Locator("input").InputValueAsync();
    }
    public async Task WaitForInput(string value)
    {
        var textInput = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        await Assertions.Expect(textInput.Locator.Locator("input")).ToHaveValueAsync(value);
    }

    public async Task<bool> GetErrorState()
    {
        // get  BackgroundColor
        LocatorNodeId Frame = _session.ResolveNodeLocator(_textIn.Locator.Page, "Frame", _textIn.NodeId);
        ILocator backgroundColorLocator = Frame.Locator.Locator(":scope > div").First;
        Color backgroundColor = await GetCssColorAsync(backgroundColorLocator, "background-color");

        bool backgroundColorErrorState;
        if (backgroundColor.EqualsRgba(fatal100) || backgroundColor.EqualsRgba(fatal60)) // Red background indicates error
        {
            backgroundColorErrorState = true;
        }
        else if (backgroundColor.EqualsRgba(white)) // White background indicates no error
        {
            backgroundColorErrorState = false;
        }
        else
        {
            throw new Exception($"Unexpected background color: {backgroundColor}");
        }


        // get TextColor
        LocatorNodeId textBox = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        ILocator textColorLocator = textBox.Locator.Locator("input").First;
        Color textColor = await GetCssColorAsync(textColorLocator, "color");

        bool textColorErrorState;
        if (textColor.EqualsRgba(white))
        {
            textColorErrorState = true;
        }
        else if (textColor.EqualsRgba(black100) || backgroundColor.EqualsRgba(black44))
        {
            textColorErrorState = false;
        }
        else
        {
            throw new Exception($"Unexpected text color: {backgroundColor}");
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
        LocatorNodeId textBox = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        ILocator textColor = textBox.Locator.Locator("input").First;

        // get Locator for BackgroundColor
        LocatorNodeId Frame = _session.ResolveNodeLocator(_textIn.Locator.Page, "Frame", _textIn.NodeId);
        ILocator backgroundColor = Frame.Locator.Locator(":scope > div").First;
        if (errorstate)
        {
            // Wait for error state
            await WaitForCssColorAsync(backgroundColor, "background-color", [fatal100, fatal60]);
            await WaitForCssColorAsync(textColor, "color", white);
        }
        else
        {
            // Wait for no error state
            await WaitForCssColorAsync(backgroundColor, "background-color", white);
            await WaitForCssColorAsync(textColor, "color", [black100, black44]);

        }
    }

    public async Task<bool> IsEnabled()
    {
        // get Locator for TextColor
        LocatorNodeId textBox = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        ILocator textColorLocator = textBox.Locator.Locator("input").First;
        Color textColor = await GetCssColorAsync(textColorLocator, "color");

        bool textColorEnableState;
        if (textColor.EqualsRgba(black100))
        {
            textColorEnableState = true;
        }
        else if (textColor.EqualsRgba(black44))
        {
            textColorEnableState = false;
        }
        else
        {
            throw new Exception($"Unexpected text color: {textColorLocator}");
        }

        // Check if keyboard will not open
        ILocator keyboardInput = textBox.Locator.Locator(":scope input").First;
        bool keyboardEnabled = await keyboardInput.IsEnabledAsync();

        if (keyboardEnabled != textColorEnableState)
        {
            throw new Exception($"Keyboard enabled state does not match text color state. Text color: {textColorLocator}, Keyboard enabled: {keyboardEnabled}");
        }

        return textColorEnableState;
    }

    public async Task<bool> WaitForEnabled(bool enabled)
    {
        Color textColor = enabled ? black100 : black44;

        // get Locator for TextColor
        LocatorNodeId textBox = _session.ResolveNodeLocator(_textIn.Locator.Page, BuildPath("Frame", "VerticalLayout", "TextInput").ToString(), _textIn.NodeId);
        ILocator textColorLocator = textBox.Locator.Locator("input").First;
        await WaitForCssColorAsync(textColorLocator, "color", textColor);



        // Check if keyboard will not open
        ILocator keyboardInput = textBox.Locator.Locator(":scope input").First;

        // Check if keyboard is enabled
        if (enabled)
        {
            await Assertions.Expect(keyboardInput)
                .ToBeEnabledAsync(new()
                {
                    Timeout = 2000
                });
            return true;

        }
        else
        {
            await Assertions.Expect(keyboardInput)
                .ToBeDisabledAsync(new()
                {
                    Timeout = 2000
                });
            return false;
        }
    }






}