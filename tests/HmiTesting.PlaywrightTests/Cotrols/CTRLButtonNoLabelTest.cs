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

public class CTRLButtonNoLabelTest : UiTestBase
{
    private CTRL_ButtonNoLabel _ctrlButtonNoLabel;

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(1);
        _ctrlButtonNoLabel = area1.getElementByName<CTRL_ButtonNoLabel>("CoT_CTRL_ButtonNoLabel");
    }

    [Test]
    public async Task TestCTRLButtonNoLabelProperties()
    {
        //Get IconName
        string Iconname = _ctrlButtonNoLabel.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to button: {Iconname}");
        //Set Icon 
        _ctrlButtonNoLabel.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlButtonNoLabel.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Set button Text
        LocalizedText textButton = _ctrlButtonNoLabel.textProperty;
        Assert.That(textButton.Text.Equals("Button"), $"Wrong text initial text for Button. Text is: {textButton}");
        //Set Text Label
        LocalizedText localizedNewTextButton = new("en-US", "New Button Text");
        _ctrlButtonNoLabel.textProperty = localizedNewTextButton;
        await _ctrlButtonNoLabel.WaitForTextProperty(localizedNewTextButton);

        // Get UserRole
        string userRole = _ctrlButtonNoLabel.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");
    }

    [Test]
    public async Task TestCTRLButtonCheckButtonExsistence()
    {
        ILocator buttonLocator = _ctrlButtonNoLabel.button._button.Locator;
        await buttonLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLButtonNoLabelVisibility()
    {
        bool visibilety = await _ctrlButtonNoLabel.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Button should be visible by default");
        //Set Button to invisible
        _ctrlButtonNoLabel.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlButtonNoLabel.WaitForVisibleAsync(false);
    }
}