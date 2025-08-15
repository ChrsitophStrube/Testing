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

public class CTRL_ButtonLedTest : UiTestBase
{
    private CTRL_ButtonLed _ctrlButton;

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(1);
        _ctrlButton = area1.getElementByName<CTRL_ButtonLed>("CoT_CTRL_ButtonLed");
    }


    [Test]
    public async Task TestCTRLButtonProperties()
    {
        //Get IconName
        string Iconname = _ctrlButton.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to button: {Iconname}");
        //Set Icon 
        _ctrlButton.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlButton.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Get Text Label
        LocalizedText textLabel = _ctrlButton.LabelTextProperty;
        Assert.That(textLabel.Text.Equals("This is a label"), $"Wrong text initial text for Label. Text is: {textLabel}");
        //Set Text Label
        LocalizedText localizedNewText = new("en-US", "New Label Text");
        _ctrlButton.LabelTextProperty = localizedNewText;
        await _ctrlButton.WaitForLabelTextProperty(localizedNewText);

        //Set button Text
        LocalizedText textButton = _ctrlButton.ButtonTextProperty;
        Assert.That(textButton.Text.Equals("Button"), $"Wrong text initial text for Button. Text is: {textLabel}");
        //Set Text Label
        LocalizedText localizedNewTextButton = new("en-US", "New Button Text");
        _ctrlButton.ButtonTextProperty = localizedNewTextButton;
        await _ctrlButton.WaitForButtonTextProperty(localizedNewTextButton);

        // Get UserRole
        string userRole = _ctrlButton.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");


    }
    [Test]
    public async Task TestCTRLButtonCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlButton.IsEnabled();
        Assert.That(isEnabled, Is.True, "Button should be enabled by default");

        //Set Button to disabled
        _ctrlButton.EnableProperty = false;
        await _ctrlButton.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        await _ctrlButton.WaitForEnabled(false);
        isEnabled = await _ctrlButton.IsEnabled();
        Assert.That(isEnabled, Is.False, "Button should be disabled now");
    }

    [Test]
    public async Task TestCTRLButtonCheckButtonExsistence()
    {

        ILocator buttonLocator = _ctrlButton.buttonLed._button.Locator;
        await buttonLocator.IsVisibleAsync();

    }

    [Test]
    public async Task TestCTRLButtonCheckLabelExsistence()
    {
        ILocator buttonLocator = _ctrlButton.label._label.Locator;
        await buttonLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLButtonCheckLabelVisibility()
    {
        bool visibilety = await _ctrlButton.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Button should be visible by default");
        //Set Button to invisible
        _ctrlButton.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlButton.WaitForVisibleAsync(false);
    }
}