using HmiTesting.OpcUa;
using Microsoft.Playwright;
using NUnit.Framework;

public abstract class UiTestBase
{
    protected IPlaywright? playwright = null;
    protected IBrowser? _browser;
    protected IPage? _page;
    protected OpcUaSession _session;


    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        playwright = await Playwright.CreateAsync();

        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            Args = new[] { "--force-device-scale-factor=1" }
        });

        //Open Page
        _page = await _browser.NewPageAsync();
        await _page.SetViewportSizeAsync(1925, 1085);
        await _page.GotoAsync(ProjectConfig.Current.ProjectUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.WaitForTimeoutAsync(2000);

        //conect to OPCUA Server
        OpcUaClient client = new OpcUaClient();
        _session = (OpcUaSession)client.Connect(ProjectConfig.Current.ProjectName, ProjectConfig.Current.OpcUaIp, ProjectConfig.Current.OpcUaPort);
    }

    [OneTimeTearDown]
    public async Task DisconnectOpcUaServer()
    {
        _session?.Disconnect();
        await _page?.CloseAsync();
        await _browser?.CloseAsync();
    }
}