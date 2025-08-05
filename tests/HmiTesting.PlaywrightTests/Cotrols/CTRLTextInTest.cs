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

public class CTRLTextInTest
{

    private IPlaywright? playwright = null;
    private IBrowser? _browser;
    private IPage? _page;
    private CTRL_TextIn _ctrlTextIn;

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
        _ctrlTextIn = area1.getElementByName<CTRL_TextIn>("CoT_CTRL_TextIn");
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
    public async Task TestCTRLButtonProperties()
    {
        //Get IconName
        string Iconname = _ctrlTextIn.IconProperty;
        Assert.That(Iconname.Equals("ico-action-done-small.svg"), $"Wrong icon Path set to Label: {Iconname}");
        //Set Icon 
        _ctrlTextIn.IconProperty = "icon_filter_small_HMI.svg";
        await _ctrlTextIn.WaitForIconProperty("icon_filter_small_HMI.svg");

        //Get Text Label
        LocalizedText textLabel = _ctrlTextIn.LabelTextProperty;
        Assert.That(textLabel.Text.Equals("This is a label"), $"Wrong text initial text for Label. Text is: {textLabel}");
        //Set Text Label
        LocalizedText localizedNewText = new("en-US", "New Label Text");
        _ctrlTextIn.LabelTextProperty = localizedNewText;
        await _ctrlTextIn.WaitForLabelTextProperty(localizedNewText);

        // Get UserRole
        string userRole = _ctrlTextIn.UserRoleProperty;
        Assert.That(userRole.Equals("Produce"), $"Wrong UserRole initial text for Button. UserRole is: {userRole}");

        //Get TextInText
        LocalizedText textInText = _ctrlTextIn.TextInTextProperty;
        Assert.That(textInText.Text.Equals("Textvalue"), $"Wrong text initial text for TextIn. Text is: {textInText}");
        //Set TextInText
        LocalizedText localizedNewTextTextIn = new("en-US", "New TextIn Text");
        _ctrlTextIn.TextInTextProperty = localizedNewTextTextIn;
        await _ctrlTextIn.WaitForTextInTextProperty(localizedNewTextTextIn);
        //Get ErrorState
        bool errorState = _ctrlTextIn.ErrorStateProperty;
        Assert.That(errorState.Equals(false), $"Wrong ErrorState initial value. Value is: {errorState}");
        //Set ErrorState
        _ctrlTextIn.ErrorStateProperty = true;
        await _ctrlTextIn.WaitForErrorStateProperty(true);

        //Get Unit
        string unit = _ctrlTextIn.UnitProperty;
        Assert.That(unit.Equals(""), $"Wrong Unit initial value. Value is: {unit}");
        //Set Unit
        _ctrlTextIn.UnitProperty = "km/h";
        await _ctrlTextIn.WaitForUnitProperty("km/h");

    }
    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLTextInCheckEnable()
    {
        //Check if Button is enabled
        bool isEnabled = await _ctrlTextIn.IsEnabled();
        Assert.That(isEnabled, Is.True, "Button should be enabled by default");

        //Set Button to disabled
        _ctrlTextIn.EnableProperty = false;
        await _ctrlTextIn.WaitForEnablePropertyAsync(false);
        //Check if Button is disabled 
        isEnabled = await _ctrlTextIn.IsEnabled();
        Assert.That(isEnabled, Is.False, "Button should be disabled now");
    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLTextIntTextInExsistence()
    {

        ILocator textInLocator = _ctrlTextIn.textIn._textIn.Locator;
        await textInLocator.IsVisibleAsync();

    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLButtonCheckLabelExsistence()
    {
        ILocator labelLocator = _ctrlTextIn.label._label.Locator;
        await labelLocator.IsVisibleAsync();
    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLButtonCheckLabelVisibility()
    {
        bool visibilety = await _ctrlTextIn.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Switch should be visible by default");
        //Set Button to invisible
        _ctrlTextIn.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlTextIn.WaitForVisibleAsync(false);
    }
}