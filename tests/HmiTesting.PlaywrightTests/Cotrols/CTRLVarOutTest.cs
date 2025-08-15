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

public class CTRLVarOutTest : UiTestBase
{
    private CTRL_VarOut<int> _ctrlVarOut;


    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(3);
        _ctrlVarOut = area1.getElementByName<CTRL_VarOut<int>>("CoT_CTRL_VarOut");
    }

    [Test]
    public async Task TestCTRLVarInOutProperties()
    {
        // Get Text
        LocalizedText textInText = _ctrlVarOut.LabelTextProperty;
        Assert.That(textInText.Text.Equals("This is a label"), $"Wrong initial TextInText. Text is: {textInText.Text}");
        // Set Text
        var newTextIn = new LocalizedText("en-US", "New VarOut Text");
        _ctrlVarOut.LabelTextProperty = newTextIn;
        await _ctrlVarOut.WaitForLabelTextProperty(newTextIn);



        // Get Value
        int varOutValue = _ctrlVarOut.ValueProperty;
        Assert.That(varOutValue.Equals(0), $"Wrong initial Value. Value is: {varOutValue}");
        // Set Value
        _ctrlVarOut.ValueProperty = 84;
        await _ctrlVarOut.WaitForValueProperty(84);


        // Get DecimalPlaces
        int decimalPlaces = _ctrlVarOut.DecimalPlacesProperty;
        Assert.That(decimalPlaces.Equals(0), $"Wrong initial DecimalPlaces. Value is: {decimalPlaces}");
        // Set DecimalPlaces
        _ctrlVarOut.DecimalPlacesProperty = 3;
        await _ctrlVarOut.WaitForDecimalPlacesProperty(3);

        // Get ErrorState
        bool errorState = _ctrlVarOut.ErrorStateProperty;
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState. Value is: {errorState}");
        // Set ErrorState
        _ctrlVarOut.ErrorStateProperty = true;
        await _ctrlVarOut.WaitForErrorStateProperty(true);

        // Get Unit
        LocalizedText unit = _ctrlVarOut.UnitProperty;
        Assert.That(unit.Text.Equals(string.Empty), $"Wrong initial Unit. Value is: {unit}");
        // Set Unit
        LocalizedText unitText = new LocalizedText("en-US", "kg");
        _ctrlVarOut.UnitProperty = unitText;
        await _ctrlVarOut.WaitForUnitProperty(unitText);

    }

    [Test]
    public async Task TestCTRLVarInOutVarInExsistence()
    {

        ILocator textInLocator = _ctrlVarOut.varOut._varOut.Locator;
        await textInLocator.IsVisibleAsync();

    }

    [Test]
    public async Task TestVarInOutCheckLabelExsistence()
    {
        ILocator labelLocator = _ctrlVarOut.label._label.Locator;
        await labelLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLVarInOutVisibility()
    {
        bool visibilety = await _ctrlVarOut.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Switch should be visible by default");
        //Set Varin to invisible
        _ctrlVarOut.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlVarOut.WaitForVisibleAsync(false);
    }
}