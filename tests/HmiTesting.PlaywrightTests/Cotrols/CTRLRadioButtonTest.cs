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

public class CTRLRadioButtonTest : UiTestBase
{

    private CTRL_Radiobutton _ctrlRadioButton;

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(2);
        _ctrlRadioButton = area1.getElementByName<CTRL_Radiobutton>("CoT_CTRL_RadioButton");
    }

    [Test]
    public async Task TestCTRLButtonProperties()
    {
        //Get IconName
        string Iconname = _ctrlRadioButton.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to button: {Iconname}");
        //Set Icon 
        _ctrlRadioButton.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlRadioButton.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Get Text Label
        LocalizedText textLabel = _ctrlRadioButton.LabelTextProperty;
        Assert.That(textLabel.Text.Equals("This is a label"), $"Wrong text initial text for Label. Text is: {textLabel}");
        //Set Text Label
        LocalizedText localizedNewText = new("en-US", "New Label Text");
        _ctrlRadioButton.LabelTextProperty = localizedNewText;
        await _ctrlRadioButton.WaitForLabelTextProperty(localizedNewText);

        // Get UserRole
        string userRole = _ctrlRadioButton.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");

        // Get SelectedOption
        Int16 selectedOption = _ctrlRadioButton.SelectedOptipnProperty;
        Assert.That(selectedOption.Equals(0), $"Wrong initial SelectedOption for Button. SelectedOption is: {selectedOption}");
        // Set SelectedOption
        _ctrlRadioButton.SelectedOptipnProperty = 1;
        await _ctrlRadioButton.WaitForSelectedOptipnProperty(1);

        // Get optionId
        Int16 optionId = _ctrlRadioButton.OptionIDProperty;
        Assert.That(optionId.Equals(0), $"Wrong initial OptionId for Button. OptionId is: {optionId}");
        // Set optionId
        _ctrlRadioButton.OptionIDProperty = 1;
        await _ctrlRadioButton.WaitForOptionIDProperty(1);

    }
    [Test]
    public async Task TestCTRLRadioButtonCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlRadioButton.IsEnabled();
        Assert.That(isEnabled, Is.True, "Button should be enabled by default");

        //Set Button to disabled
        _ctrlRadioButton.EnableProperty = false;
        await _ctrlRadioButton.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        await _ctrlRadioButton.WaitForEnabled(false);
        isEnabled = await _ctrlRadioButton.IsEnabled();
        Assert.That(isEnabled, Is.False, "Button should be disabled now");
    }

    [Test]
    public async Task TestCTRLRadioButtonCheckButtonExsistence()
    {

        ILocator buttonLocator = _ctrlRadioButton.radiobutton._radiobutton.Locator;
        await buttonLocator.IsVisibleAsync();

    }

    [Test]
    public async Task TestCTRLRadioButtonCheckLabelExsistence()
    {
        ILocator buttonLocator = _ctrlRadioButton.label._label.Locator;
        await buttonLocator.IsVisibleAsync();
    }

    [Test]
    public async Task TestCTRLRadioButtonCheckLabelVisibility()
    {
        bool visibilety = await _ctrlRadioButton.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Button should be visible by default");
        //Set Button to invisible
        _ctrlRadioButton.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlRadioButton.WaitForVisibleAsync(false);
        
        _ctrlRadioButton.VisisbleProperty = true;
    }
    [Test]
    public async Task TestCTRLRadioButtonCheckRadioButton()
    {
        //Set RadioButton to unchecked
        _ctrlRadioButton.SelectedOptipnProperty = 1;
        _ctrlRadioButton.OptionIDProperty = 0;
        await _ctrlRadioButton.WaitForSelectedOptipnProperty(1);
        await _ctrlRadioButton.WaitForOptionIDProperty(0);

        //Check if RadioButton is checked
        bool isChecked = await _ctrlRadioButton.radiobutton.GetChecked();
        Assert.That(isChecked, Is.False, "RadioButton should not be checked by default");

        //Set RadioButton to checked
        _ctrlRadioButton.SelectedOptipnProperty = 1;
        _ctrlRadioButton.OptionIDProperty = 1;
        await _ctrlRadioButton.WaitForSelectedOptipnProperty(1);
        await _ctrlRadioButton.WaitForOptionIDProperty(1);

        //Check if RadioButton is checked
        isChecked = await _ctrlRadioButton.radiobutton.GetChecked();
        Assert.That(isChecked, Is.True, "RadioButton should be checked now");

    }
}