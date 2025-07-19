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

public class CMPVarIn<T> : CMPInput where T : INumber<T>
{
    protected IOpcUaSession _session;
    public LocatorNodeId _varIn { get; }

    private string _keyboardName;

    public CMPVarIn(IOpcUaSession session, LocatorNodeId varin) : base(session, varin)
    {
        _session = session;
        _varIn = varin;

        _keyboardName =
            typeof(T) == typeof(int) ? "CoT_NumericInt" :
            typeof(T) == typeof(double) ? "CoT_NumericFloat" :
            throw new NotSupportedException(
            $"Typ {typeof(T)} is not suported in CMPVarIn.");

    }

    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public Task WaitForErrorStatePropertyAsync(bool errorState) => WaitForProperty<bool>("errorState", errorState);

    public T MinValueProperty { get => GetProperty<T>("minimum"); set => SetProperty("minimum", value); }
    public Task WaitForMinValuePropertyAsync(T maxValue) => WaitForProperty<T>("minimum", maxValue);

    public T MaxValueProperty { get => GetProperty<T>("maximum"); set => SetProperty("maximum", value); }
    public Task WaitForMaxValuePropertyAsync(T minValue) => WaitForProperty<T>("maximum", minValue);

    public int DecimalPlacesProperty { get => GetProperty<int>("decimalPlaces"); set => SetProperty("decimalPlaces", value); }
    public Task WaitForDecimalPlacesyAsync(int decimalPlaces) => WaitForProperty<int>("decimalPlaces", decimalPlaces);


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

    public async Task WaitForValue(T value)
    {
        var spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        var expected = value.ToString(null, CultureInfo.InvariantCulture);

        await Assertions.Expect(spinBox.Locator.Locator("input")).ToHaveValueAsync(expected);
    }

    public async Task<bool> GetErrorState()
    {
        LocatorNodeId Frame = _session.ResolveNodeLocator(_varIn.Locator.Page, "Frame", _varIn.NodeId);
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
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        ILocator textColorLocator = spinBox.Locator.Locator("input").First;
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
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        ILocator textColor = spinBox.Locator.Locator("input").First;

        // get Locator for BackgroundColor
        LocatorNodeId Frame = _session.ResolveNodeLocator(_varIn.Locator.Page, "Frame", _varIn.NodeId);
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
        // get TextColor
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        ILocator textColorLocator = spinBox.Locator.Locator("input").First;
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
            throw new Exception($"Unexpected text color: {textColor}");
        }

        // Check if keyboard will not open
        ILocator keyboardInput = spinBox.Locator.Locator(":scope input").First;
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
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_varIn.Locator.Page, BuildPath("Frame", "VarInput").ToString(), _varIn.NodeId);
        ILocator textColorLocator = spinBox.Locator.Locator("input").First;
        await WaitForCssColorAsync(textColorLocator, "color", textColor);



        // Check if keyboard will not open
        ILocator keyboardInput = spinBox.Locator.Locator(":scope input").First;

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