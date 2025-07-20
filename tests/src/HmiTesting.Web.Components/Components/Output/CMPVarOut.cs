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
public class CMPVarOut<T> : CMPOutput where T : INumber<T>
{
    private IOpcUaSession _session;
    public LocatorNodeId _varOut { get; }
    public CMPVarOut(IOpcUaSession session, LocatorNodeId varOut) : base(session, varOut)
    {
        _session = session;
        _varOut = varOut;
    }

    public string UnitProperty { get => GetProperty<string>("unit"); set => SetProperty("unit", value); }
    public async Task WaitForUnitProperty(string unit) => await WaitForProperty<string>("unit", unit);

    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public async Task WaitForErrorStateProperty(bool errorstate) => await WaitForProperty<bool>("errorState", errorstate);

    public int DecimalPlacesProperty { get => GetProperty<int>("decimalPlaces"); set => SetProperty("decimalPlaces", value); }
    public async Task WaitForDecimalPlacesProperty(int decimalPlaces) => await WaitForProperty<int>("decimalPlaces", decimalPlaces);

    public T ValueProperty { get => GetProperty<T>("value"); set => SetProperty("value", value); }

    public async Task WaitForValueProperty(T value) => await WaitForProperty<T>("value", value);

    public async Task<T> GetValue()
    {
        var spinBox = _session.ResolveNodeLocator(_varOut.Locator.Page, "NumDisplay", _varOut.NodeId);
        string value = await spinBox.Locator.Locator("span").InnerTextAsync();

        string cleanedValue = value.EndsWith(".0", StringComparison.Ordinal)
                 ? value[..^2]               // "42.0" → "42"
                 : value;

        return T.Parse(
            cleanedValue,
            CultureInfo.InvariantCulture);
    }

    public async Task WaitForValue(T value)
    {
        var spinBox = _session.ResolveNodeLocator(_varOut.Locator.Page, "NumDisplay", _varOut.NodeId);
        var expected = value.ToString(null, CultureInfo.InvariantCulture);

        await Assertions.Expect(spinBox.Locator.Locator("input")).ToHaveValueAsync(expected);
    }

    public async Task<bool> GetErrorState()
    {

        // get TextColor
        LocatorNodeId label = _session.ResolveNodeLocator(_varOut.Locator.Page, "NumDisplay", _varOut.NodeId);
        ILocator textColorLocator = label.Locator.Locator("span").First;
        Color textColor = await GetCssColorAsync(textColorLocator, "color");

        bool textColorErrorState;
        if (textColor.EqualsRgba(fatal100))
        {
            textColorErrorState = true;
        }
        else if (textColor.EqualsRgba(black100))
        {
            textColorErrorState = false;
            return false; // White text color indicates no error 
        }
        else
        {
            throw new Exception($"Unexpected text color: {textColor}");
        }

        // get Color of BottomLine
        LocatorNodeId bottomLine = _session.ResolveNodeLocator(_varOut.Locator.Page, "BottomLine", _varOut.NodeId);
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
        LocatorNodeId spinBox = _session.ResolveNodeLocator(_varOut.Locator.Page, "NumDisplay", _varOut.NodeId);
        ILocator textColorLocator = spinBox.Locator.Locator("span").First;

        // get Locator for BackgroundColor
        LocatorNodeId bottomLine = _session.ResolveNodeLocator(_varOut.Locator.Page, "BottomLine", _varOut.NodeId);
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
            await WaitForCssColorAsync(textColorLocator, "color", black100);
        }
    }



}