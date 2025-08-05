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

public class CTRLTextOutTest
{

    private IPlaywright? playwright = null;
    private IBrowser? _browser;
    private IPage? _page;
    private CTRL_TextOut _ctrlTextOut;

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
        await _page.GotoAsync("http://192.168.1.200:50080", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.EvaluateAsync("() => { document.body.style.zoom = '80%'; }");
        await _page.WaitForTimeoutAsync(2000);

        //conect to OPCUA Server
        OpcUaClient client = new OpcUaClient();
        _session = (OpcUaSession)client.Connect("MyHMI_Template_Unencrypted", "192.168.1.200", 59100);
    }

    [SetUp]
    public async Task Setup()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        var componentsDevpage = BuildPath("TestScreens", "Controls");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ControlsOverview");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(3);
        _ctrlTextOut = area1.getElementByName<CTRL_TextOut>("CoT_CTRL_TextOut");
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
    public async Task TestCTRLVarInOutProperties()
    {
        // Get Text
        LocalizedText textInText = _ctrlTextOut.LabelTextProperty;
        Assert.That(textInText.Text.Equals("This is a label"), $"Wrong initial TextInText. Text is: {textInText.Text}");
        // Set Text
        var newTextIn = new LocalizedText("en-US", "New VarOut Text");
        _ctrlTextOut.LabelTextProperty = newTextIn;
        await _ctrlTextOut.WaitForLabelTextProperty(newTextIn);

        // Get Value
        LocalizedText textOutText = _ctrlTextOut.TextOutProperty;
        Assert.That(textOutText.Text.Equals("Textvalue"), $"Wrong initial Value. Value is: {textOutText}");
        // Set Value
        textOutText = new LocalizedText("en-US", "Honigkuchen");
        _ctrlTextOut.TextOutProperty = textOutText;
        await _ctrlTextOut.WaitForTextOutProperty(textOutText);

        // Get ErrorState
        bool errorState = _ctrlTextOut.ErrorStateProperty;
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState. Value is: {errorState}");
        // Set ErrorState
        _ctrlTextOut.ErrorStateProperty = true;
        await _ctrlTextOut.WaitForErrorStateProperty(true);

        // Get Unit
        string unit = _ctrlTextOut.UnitProperty;
        Assert.That(unit.Equals(string.Empty), $"Wrong initial Unit. Value is: {unit}");
        // Set Unit
        _ctrlTextOut.UnitProperty = "kg";
        await _ctrlTextOut.WaitForUnitProperty("kg");

    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLVarInOutVarInExsistence()
    {

        ILocator textInLocator = _ctrlTextOut.textOut._textOut.Locator;
        await textInLocator.IsVisibleAsync();

    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestVarInOutCheckLabelExsistence()
    {
        ILocator labelLocator = _ctrlTextOut.label._label.Locator;
        await labelLocator.IsVisibleAsync();
    }

    [Test]
    [Ignore("Temporary deactivated for faster CI/CD")]
    public async Task TestCTRLVarInOutVisibility()
    {
        bool visibilety = await _ctrlTextOut.GetVisibleAsync();
        Assert.That(visibilety, Is.True, "Switch should be visible by default");
        //Set Varin to invisible
        _ctrlTextOut.VisisbleProperty = false;
        //Check if Button is invisible
        await _ctrlTextOut.WaitForVisibleAsync(false);
    }
}