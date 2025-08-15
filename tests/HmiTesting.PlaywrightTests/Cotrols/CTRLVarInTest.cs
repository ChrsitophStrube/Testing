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

public class CTRLVarInTest : UiTestBase
{

    private CTRL_VarIn<int> _ctrlVarIn;

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(2);
        _ctrlVarIn = area1.getElementByName<CTRL_VarIn<int>>("CoT_CTRL_VarIn");
    }

    [Test]
    public async Task TestCTRLVarInProperties()
    {
        // Get Text
        LocalizedText textInText = _ctrlVarIn.TextProperty;
        Assert.That(textInText.Text.Equals("This is a label"), $"Wrong initial TextInText. Text is: {textInText.Text}");
        // Set Text
        var newTextIn = new LocalizedText("en-US", "New VarIn Text");
        _ctrlVarIn.TextProperty = newTextIn;
        await _ctrlVarIn.WaitForTextProperty(newTextIn);

        // Get Value
        int value = _ctrlVarIn.ValueProperty;
        Assert.That(value.Equals(0), $"Wrong initial Value. Value is: {value}");
        // Set Value
        _ctrlVarIn.ValueProperty = 42;
        await _ctrlVarIn.WaitForValueProperty(42);

        // Get DecimalPlaces
        int decimalPlaces = _ctrlVarIn.DecimalPlacesProperty;
        Assert.That(decimalPlaces.Equals(0), $"Wrong initial DecimalPlaces. Value is: {decimalPlaces}");
        // Set DecimalPlaces
        _ctrlVarIn.DecimalPlacesProperty = 3;
        await _ctrlVarIn.WaitForDecimalPlacesProperty(3);

        // Get ErrorState
        bool errorState = _ctrlVarIn.ErrorStateProperty;
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState. Value is: {errorState}");
        // Set ErrorState
        _ctrlVarIn.ErrorStateProperty = true;
        await _ctrlVarIn.WaitForErrorStateProperty(true);

        // Get Unit
        LocalizedText unit = _ctrlVarIn.UnitProperty;
        Assert.That(unit.Text.Equals(string.Empty), $"Wrong initial Unit. Value is: {unit}");
        // Set Unit
        LocalizedText unitText = new LocalizedText("en-US", "kg");
        _ctrlVarIn.UnitProperty = unitText;
        await _ctrlVarIn.WaitForUnitProperty(unitText);

        // Get Minimum
        int minimum = _ctrlVarIn.MinimumProperty;
        Assert.That(minimum.Equals(-2147483648), $"Wrong initial Minimum. Value is: {minimum}");
        // Set Minimum
        _ctrlVarIn.MinimumProperty = 1;
        await _ctrlVarIn.WaitForMinimumProperty(1);

        // Get Maximum
        int maximum = _ctrlVarIn.MaximumProperty;
        Assert.That(maximum.Equals(2147483647), $"Wrong initial Maximum. Value is: {maximum}");
        // Set Maximum
        _ctrlVarIn.MaximumProperty = 10;
        await _ctrlVarIn.WaitForMaximumProperty(10);

        // Enabled state
        bool isEnabled = _ctrlVarIn.EnableProperty;
        Assert.That(isEnabled, Is.True, "VarIn should be enabled by default");
        // Set VarIn to disabled
        _ctrlVarIn.EnableProperty = false;
        await _ctrlVarIn.WaitForEnablePropertyAsync(false);
        _ctrlVarIn.EnableProperty = true;
    }

    [Test]
    public async Task TestCTRLVarInCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlVarIn.IsEnabled();
        Assert.That(isEnabled, Is.True, "VarIn should be enabled by default");

        //Set Button to disabled
        _ctrlVarIn.EnableProperty = false;
        await _ctrlVarIn.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        await _ctrlVarIn.WaitForEnabled(false);
        isEnabled = await _ctrlVarIn.IsEnabled();
        Assert.That(isEnabled, Is.False, "VarIn should be disabled now");
        _ctrlVarIn.EnableProperty = true;
    }

    [Test]
    public async Task TestCTRLVarIntVarInExsistence()
    {

        ILocator textInLocator = _ctrlVarIn.VarIn._varIn.Locator;
        await textInLocator.IsVisibleAsync();

    }

    [Test]
    public async Task TestCTRLButtonCheckLabelExsistence()
    {
        ILocator labelLocator = _ctrlVarIn.label._label.Locator;
        await labelLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLButtonCheckLabelVisibility()
    {
        bool visibilety = await _ctrlVarIn.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Switch should be visible by default");
        //Set Button to invisible
        _ctrlVarIn.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlVarIn.WaitForVisibleAsync(false);
        _ctrlVarIn.VisisbleProperty = true;
    }
}