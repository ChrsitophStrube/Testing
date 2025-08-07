namespace HmiTesting.PlaywrightTests;

using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using HmiTesting.OpcUa;
using System.Threading.Tasks;
using HmiTesting.Web.Navigation;
using HmiTesting.Web.Components;
using LibUA.Core;
using HmiTesting.Web.Pages;
using HmiTesting.Core.Helpers;

public class ScreenshotAllPages
{

    private IPlaywright? playwright = null;
    private IBrowser? _browser;
    private IPage? _page;
    private CTRL_ButtonLedNoLabel _ctrlButtonLedNoLabel;

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
        await _page.GotoAsync("http://localhost:8080", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.EvaluateAsync("() => { document.body.style.zoom = '90%'; }");
        await _page.WaitForTimeoutAsync(2000);

        //conect to OPCUA Server
        OpcUaClient client = new OpcUaClient();
        _session = (OpcUaSession)client.Connect("MyHMI_Template_Unencrypted");
    }

    [OneTimeTearDown]
    public async Task DisconnectOpcUaServer()
    {
        _session?.Disconnect();
        await _page?.CloseAsync();
        await _browser?.CloseAsync();
    }

    [Test]
    public async Task ScreenshotsOfAllPages()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        Dictionary<OpcPath, string> screens = [];
        // screens.Add(BuildPath("Machine settings", "MODX1", "Belt", "General"), "DemoModX_MS_Belt_General");
        // screens.Add(BuildPath("Administer", "Device server", "Runtime"), "CoTD_Server_Runtime");
        //screens.Add(BuildPath("Administer", "SSI"), "CoTD_SSI");
        screens = NaxigationPaser.GetScreensFromNavigationXML(@"D:\13_Masterarbeit\Repos\MAShmi\MAS-HMI\ProjectFiles\NavigationContent.xml");



        // IHmiPage _motorGeneralPage = await _session.Navigator(_page).GoToPage(componentsDevpage, "DemoModX_MS_Belt_General",true);
        var results = await _session.Navigator(_page).GoToAllPages(screens, MakePageScreenshot);

    }

}