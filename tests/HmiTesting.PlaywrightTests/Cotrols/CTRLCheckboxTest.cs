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

public class CTRLCheckboxTest
{

    private IPlaywright? playwright = null;
    private IBrowser? _browser;
    private IPage? _page;
    private CTRL_CheckBox _ctrlCheckbox;
    private OpcUaSession _session;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        playwright = await Playwright.CreateAsync();

        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false
        });

        //Open Page
        _page = await _browser.NewPageAsync();
        await _page.SetViewportSizeAsync(1920, 1080);
        await _page.GotoAsync("http://:192.168.1.201:50080", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.EvaluateAsync("() => { document.body.style.zoom = '80%'; }");
        await _page.WaitForTimeoutAsync(2000);

        //conect to OPCUA Server
        OpcUaClient client = new OpcUaClient();
        _session = (OpcUaSession)client.Connect("MyHMI_Template_Unencrypted", "192.168.1.201", 59100);
    }

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(2);
        _ctrlCheckbox = area1.getElementByName<CTRL_CheckBox>("CoT_CTRL_CheckBox");
    }

    [OneTimeTearDown]
    public async Task DisconnectOpcUaServer()
    {
        _session?.Disconnect();
        await _page?.CloseAsync();
        await _browser?.CloseAsync();
    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLCheckBoxProperties()
    {
        //Get IconName
        string Iconname = _ctrlCheckbox.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to button: {Iconname}");
        //Set Icon 
        _ctrlCheckbox.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlCheckbox.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Get Text Label
        LocalizedText textLabel = _ctrlCheckbox.LabelTextProperty;
        Assert.That(textLabel.Text.Equals("This is a label"), $"Wrong text initial text for Label. Text is: {textLabel}");
        //Set Text Label
        LocalizedText localizedNewText = new("en-US", "New Label Text");
        _ctrlCheckbox.LabelTextProperty = localizedNewText;
        await _ctrlCheckbox.WaitForLabelTextProperty(localizedNewText);

        // Get UserRole
        string userRole = _ctrlCheckbox.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");

        //Get CheckBoxPosition
        Int32 checkBoxPosition = _ctrlCheckbox.CheckBoxPositionProperty;
        Assert.That(checkBoxPosition.Equals(1), $"Wrong CheckBoxPosition initial value. Value is: {checkBoxPosition}");
        //Set CheckBoxPosition
        _ctrlCheckbox.CheckBoxPositionProperty = 0;
        await _ctrlCheckbox.WaitForCheckBoxPositionProperty(0);

        //Get Checked
        bool checkedValue = _ctrlCheckbox.CheckedProperty;
        Assert.That(checkedValue.Equals(false), $"Wrong Checked initial value. Value is: {checkedValue}");
        //Set Checked
        _ctrlCheckbox.CheckedProperty = true;
        await _ctrlCheckbox.WaitForCheckedProperty(true);

    }
    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLRadioButtonCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlCheckbox.IsEnabled();
        Assert.That(isEnabled, Is.True, "Button should be enabled by default");

        //Set Button to disabled
        _ctrlCheckbox.EnableProperty = false;
        await _ctrlCheckbox.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        isEnabled = await _ctrlCheckbox.IsEnabled();
        Assert.That(isEnabled, Is.False, "Button should be disabled now");
    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLCheckboxCheckCheckboxExsistence()
    {
        ILocator checkboxLocator;
        if (_ctrlCheckbox.CheckBoxPositionProperty == 1)
        {
            checkboxLocator = _ctrlCheckbox.checkBoxRight._checkbox.Locator;
        }
        else
        {
            checkboxLocator = _ctrlCheckbox.checkBoxLeft._checkbox.Locator;
        }

        await checkboxLocator.IsVisibleAsync();

    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLCheckboxCheckLabelExsistence()
    {
        ILocator buttonLocator = _ctrlCheckbox.label._label.Locator;
        await buttonLocator.IsVisibleAsync();
    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLCheckboxCheckLabelVisibility()
    {
        bool visibilety = await _ctrlCheckbox.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Checkbox should be visible by default");
        //Set Button to invisible
        _ctrlCheckbox.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlCheckbox.WaitForVisibleAsync(false);
    }
}