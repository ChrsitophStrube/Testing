namespace HmiTesting.PlaywrightTests;

using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using HmiTesting.OpcUa;
using System.Threading.Tasks;
using HmiTesting.Web.Navigation;
using HmiTesting.Web.Components;
using LibUA.Core;
using HmiTesting.Web.Pages;

public class CTRLVarInOutTest : UiTestBase
{
    private CTRL_VarInOut<int> _ctrlVarInOut;

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(3);
        _ctrlVarInOut = area1.getElementByName<CTRL_VarInOut<int>>("CoT_CTRL_VarInOut");
    }

    [Test]
    public async Task TestCTRLVarInOutProperties()
    {
        // Get Text
        LocalizedText textInText = _ctrlVarInOut.TextProperty;
        Assert.That(textInText.Text.Equals("This is a label"), $"Wrong initial TextInText. Text is: {textInText.Text}");
        // Set Text
        var newTextIn = new LocalizedText("en-US", "New VarIn Text");
        _ctrlVarInOut.TextProperty = newTextIn;
        await _ctrlVarInOut.WaitForTextProperty(newTextIn);

        // Get VarInValue
        int varInValue = _ctrlVarInOut.VarInValueProperty;
        Assert.That(varInValue.Equals(0), $"Wrong initial VarInValue. Value is: {varInValue}");
        // Set VarInValue
        _ctrlVarInOut.VarInValueProperty = 42;
        await _ctrlVarInOut.WaitForVarInValueProperty(42);

        // Get VarOutValue
        int varOutValue = _ctrlVarInOut.VarOutValueProperty;
        Assert.That(varOutValue.Equals(0), $"Wrong initial VarOutValue. Value is: {varOutValue}");
        // Set VarOutValue
        _ctrlVarInOut.VarOutValueProperty = 84;
        await _ctrlVarInOut.WaitForVarOutValueProperty(84);


        // Get DecimalPlaces
        int decimalPlaces = _ctrlVarInOut.DecimalPlacesProperty;
        Assert.That(decimalPlaces.Equals(0), $"Wrong initial DecimalPlaces. Value is: {decimalPlaces}");
        // Set DecimalPlaces
        _ctrlVarInOut.DecimalPlacesProperty = 3;
        await _ctrlVarInOut.WaitForDecimalPlacesProperty(3);

        // Get ErrorState
        bool errorState = _ctrlVarInOut.ErrorStateProperty;
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState. Value is: {errorState}");
        // Set ErrorState
        _ctrlVarInOut.ErrorStateProperty = true;
        await _ctrlVarInOut.WaitForErrorStateProperty(true);

        // Get Unit
        LocalizedText unit = _ctrlVarInOut.UnitProperty;
        Assert.That(unit.Text.Equals(string.Empty), $"Wrong initial Unit. Value is: {unit}");
        // Set Unit
        LocalizedText unitText = new LocalizedText("en-US", "kg");
        _ctrlVarInOut.UnitProperty = unitText;
        await _ctrlVarInOut.WaitForUnitProperty(unitText);

        // Get Minimum
        int minimum = _ctrlVarInOut.MinimumProperty;
        Assert.That(minimum.Equals(-2147483648), $"Wrong initial Minimum. Value is: {minimum}");
        // Set Minimum
        _ctrlVarInOut.MinimumProperty = 1;
        await _ctrlVarInOut.WaitForMinimumProperty(1);

        // Get Maximum
        int maximum = _ctrlVarInOut.MaximumProperty;
        Assert.That(maximum.Equals(2147483647), $"Wrong initial Maximum. Value is: {maximum}");
        // Set Maximum
        _ctrlVarInOut.MaximumProperty = 10;
        await _ctrlVarInOut.WaitForMaximumProperty(10);

        // Enabled state
        _ctrlVarInOut.EnableProperty =true;
        bool isEnabled = _ctrlVarInOut.EnableProperty;
        Assert.That(isEnabled, Is.True, "VarIn should be enabled by default");
        // Set VarIn to disabled
        _ctrlVarInOut.EnableProperty = false;
        await _ctrlVarInOut.WaitForEnablePropertyAsync(false);
    }

    [Test]
    public async Task TestCTRLVarInOutCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlVarInOut.IsEnabled();
        Assert.That(isEnabled, Is.True, "VarIn should be enabled by default");

        //Set Button to disabled
        _ctrlVarInOut.EnableProperty = false;
        await _ctrlVarInOut.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        isEnabled = await _ctrlVarInOut.IsEnabled();
        Assert.That(isEnabled, Is.False, "VarIn should be disabled now");
    }

    [Test]
    public async Task TestCTRLVarInOutVarInExsistence()
    {

        ILocator textInLocator = _ctrlVarInOut.VarIn._varIn.Locator;
        await textInLocator.IsVisibleAsync();

    }

    [Test]
    public async Task TestVarInOutCheckLabelExsistence()
    {
        ILocator labelLocator = _ctrlVarInOut.label._label.Locator;
        await labelLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLVarInOutVisibility()
    {
        bool visibilety = await _ctrlVarInOut.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Switch should be visible by default");
        //Set Varin to invisible
        _ctrlVarInOut.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlVarInOut.WaitForVisibleAsync(false);
    }
}