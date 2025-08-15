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

public class CTRLSwitchTest : UiTestBase
{

    private CTRL_Switch _ctrlSwitch;




    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(2);
        _ctrlSwitch = area1.getElementByName<CTRL_Switch>("CoT_CTRL_Switch");
    }

    [Test]
    public async Task TestCTRLButtonProperties()
    {
        //Get IconName
        string Iconname = _ctrlSwitch.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to button: {Iconname}");
        //Set Icon 
        _ctrlSwitch.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlSwitch.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Get Text Label
        LocalizedText textLabel = _ctrlSwitch.LabelTextProperty;
        Assert.That(textLabel.Text.Equals("This is a label"), $"Wrong text initial text for Label. Text is: {textLabel}");
        //Set Text Label
        LocalizedText localizedNewText = new("en-US", "New Label Text");
        _ctrlSwitch.LabelTextProperty = localizedNewText;
        await _ctrlSwitch.WaitForLabelTextProperty(localizedNewText);

        // Get UserRole
        string userRole = _ctrlSwitch.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");

        //Get Command
        bool command = _ctrlSwitch.CommandProperty;
        Assert.That(command.Equals(false), $"Wrong Command initial value. Value is: {command}");
        //Set Command
        _ctrlSwitch.CommandProperty = true;
        await _ctrlSwitch.WaitForCommandProperty(true);

    }
    [Test]
    public async Task TestCTRLButtonCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlSwitch.IsEnabled();
        Assert.That(isEnabled, Is.True, "Button should be enabled by default");

        //Set Button to disabled
        _ctrlSwitch.EnableProperty = false;
        await _ctrlSwitch.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        await _ctrlSwitch.WaitForEnabled(false);
        isEnabled = await _ctrlSwitch.IsEnabled();
        Assert.That(isEnabled, Is.False, "Button should be disabled now");
    }

    [Test]
    public async Task TestCTRLButtonCheckButtonExsistence()
    {

        ILocator buttonLocator = _ctrlSwitch.@switch._switch.Locator;
        await buttonLocator.IsVisibleAsync();

    }

    [Test]
    public async Task TestCTRLButtonCheckLabelExsistence()
    {
        ILocator buttonLocator = _ctrlSwitch.label._label.Locator;
        await buttonLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLButtonCheckLabelVisibility()
    {
        bool visibilety = await _ctrlSwitch.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Switch should be visible by default");
        //Set Button to invisible
        _ctrlSwitch.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlSwitch.WaitForVisibleAsync(false);
    }
}