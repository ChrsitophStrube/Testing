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

public class CTRLButtonLedNoLabelTest : UiTestBase
{

    private CTRL_ButtonLedNoLabel _ctrlButtonLedNoLabel;

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(1);
        _ctrlButtonLedNoLabel = area1.getElementByName<CTRL_ButtonLedNoLabel>("CoT_CTRL_ButtonLedNoLabel");
    }



    [Test]
    public async Task TestCTRLButtonNoLabelProperties()
    {
        //Get IconName
        string Iconname = _ctrlButtonLedNoLabel.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to button: {Iconname}");
        //Set Icon 
        _ctrlButtonLedNoLabel.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlButtonLedNoLabel.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Set button Text
        LocalizedText textButton = _ctrlButtonLedNoLabel.textProperty;
        Assert.That(textButton.Text.Equals("Button"), $"Wrong text initial text for Button. Text is: {textButton}");
        //Set Text Label
        LocalizedText localizedNewTextButton = new("en-US", "New Button Text");
        _ctrlButtonLedNoLabel.textProperty = localizedNewTextButton;
        await _ctrlButtonLedNoLabel.WaitForTextProperty(localizedNewTextButton);

        // Get UserRole
        string userRole = _ctrlButtonLedNoLabel.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");

        // Get LedState
        int ledState = _ctrlButtonLedNoLabel.LedStateProperty;
        Assert.That(ledState.Equals(0), $"Wrong initial LedState for Button. LedState is: {ledState}");
        // Set LedState
        _ctrlButtonLedNoLabel.LedStateProperty = 1;
        await _ctrlButtonLedNoLabel.WaitForLedStateProperty(1);
    }

    [Test]
    public async Task TestCTRLButtonCheckButtonExsistence()
    {
        ILocator buttonLocator = _ctrlButtonLedNoLabel.button._button.Locator;
        await buttonLocator.IsVisibleAsync();
    }


    [Test]
    public async Task TestCTRLButtonNoLabelVisibility()
    {
        bool visibilety = await _ctrlButtonLedNoLabel.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Button should be visible by default");
        //Set Button to invisible
        _ctrlButtonLedNoLabel.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlButtonLedNoLabel.WaitForVisibleAsync(false);
    }

    [Test]
    public async Task TestCTRLButtonLed()
    {
        _ctrlButtonLedNoLabel.LedStateProperty = 3;
        await _ctrlButtonLedNoLabel.WaitForLedStateProperty(3);
        string ledStateName = await _ctrlButtonLedNoLabel.button.GetLedState();
        Assert.That(ledStateName.Equals("item=led,state=on-error,mode=enabled"), $"Wrong LedState Name. LedState is: {ledStateName}");
        _ctrlButtonLedNoLabel.LedStateProperty = 0;
    }
}