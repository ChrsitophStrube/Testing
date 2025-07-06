using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
using System.Numerics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Drawing;

public class CMPVarIn<T> : IInputControl where T : INumber<T>
{
    protected IOpcUaSession _session;
    public LocatorNodeId _varIn { get; }

    Color fatal100 = ColorTranslator.FromHtml("#D20000");
    Color fatal60 = ColorTranslator.FromHtml("#e26464");
    Color white = ColorTranslator.FromHtml("#FFFFFF");
    Color black44 = ColorTranslator.FromHtml("#8f8f8f");
    Color black100 = ColorTranslator.FromHtml("#000000");

    private string _keyboardName;

    public CMPVarIn(IOpcUaSession session, LocatorNodeId varin)
    {
        _session = session;
        _varIn = varin;

        _keyboardName =
            typeof(T) == typeof(int) ? "CoT_NumericInt" :
            typeof(T) == typeof(double) ? "CoT_NumericFloat" :
            throw new NotSupportedException(
            $"Typ {typeof(T)} is not suported in CMPVarIn.");

    }

    public async Task Click()
    {
        var button = _session.ResolveNodeLocator(_varIn.Locator.Page, "TransparentButton", _varIn.NodeId);
        await button.Locator.ClickAsync();
    }

    public async Task SetValueByKeyboard(T value)
    {
        //OpenKeyboard
        var spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        await spinBox.Locator.ClickAsync();

        //getKeyboard
        LocatorNodeId keyboard = _session.ResolveNodeLocator(_varIn.Locator.Page, _keyboardName, _session.GetLastSession());

        //get Preedit Spinbox
        LocatorNodeId preeditSpinbox = _session.ResolveNodeLocator(_varIn.Locator.Page, "PreeditTextInput", keyboard.NodeId);

        //Set Value
        await preeditSpinbox.Locator.Locator("input").PressAsync("Delete");
        await preeditSpinbox.Locator.Locator("input").FillAsync(value.ToString("0.#####", CultureInfo.InvariantCulture));

        //Close Keyboard by clicking enter
        LocatorNodeId enterButton = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("VerticalLayout1", "KeyLayout1", "Enter").ToString(), keyboard.NodeId);
        await enterButton.Locator.ClickAsync();
    }
    public async Task<T> GetValue()
    {
        var spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        string value = await spinBox.Locator.Locator("input").InputValueAsync();

        string cleanedValue = value.EndsWith(".0", StringComparison.Ordinal)
                 ? value[..^2]               // "42.0" → "42"
                 : value;

        return T.Parse(
            cleanedValue,
            CultureInfo.InvariantCulture);
    }

    public async Task<bool> GetErrorState()
    {
        Color backgroundColor = await GetBackgroundColor();

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

        Color textColor = await GetTextColor();

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

    private async Task<Color> GetTextColor()
    {
        // Check Text Color
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        string textColorRGBA = await spinBox.Locator.Locator("input").EvaluateAsync<string>(
            "el => window.getComputedStyle(el).color"
        );
        Color textColor = ParseCssColor(textColorRGBA);
        return textColor;
    }

    private async Task<Color> GetBackgroundColor()
    {
        //Check Background Color
        LocatorNodeId Frame = _session.ResolveNodeLocator(_varIn.Locator.Page, "Frame", _varIn.NodeId);
        string backgroundColorRGB = await Frame.Locator.Locator(":scope > div").First.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).backgroundColor"
        );
        Color backgroundColor = ParseCssColor(backgroundColorRGB);
        return backgroundColor;
    }


    public string GetUserRole()
    {
        LocatorNodeId iconpathLocator = _session.ResolveNodeLocator(_varIn.Locator.Page, "userRole", _varIn.NodeId);
        NodeId userRoleId = _session.GetValue<NodeId>(iconpathLocator.NodeId);
        return _session.GetBrowsename(userRoleId);
    }

    public async Task<bool> IsEnabled()
    {
        // Check if text color is disabled
        Color textColor = await GetTextColor();

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
            throw new Exception($"Unexpected text color: {textColor}");
        }

        // Check if keyboard will not open
        var spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        ILocator keyboardInput = spinBox.Locator.Locator(":scope input").First;

        try
        {
            if (textColorEnableState)
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
        catch (PlaywrightException ex)
        {
            throw new Exception($"Keyboard enabled state assertion failed for VarIn. Expected enabled: {textColorEnableState}. Text color: {textColor}. Details: {ex.Message}", ex);
        }

    }

    public async Task<bool> IsVisibleAsync(int timeoutMs = 2000)
    {
        NodeId visebiletyId = _session.GetNodeIdFromPath("visibility", _varIn.NodeId);
        bool visebilety = _session.GetValue<bool>(visebiletyId);
        WaitForSelectorState state = visebilety ? WaitForSelectorState.Visible : WaitForSelectorState.Detached;
        await _varIn.Locator.WaitForAsync(new()
        {
            State = state,
            Timeout = timeoutMs
        });
        return visebilety;
    }

}