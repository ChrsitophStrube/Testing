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

public class UsertestStefan


{

    private IPlaywright? playwright = null;
    private IBrowser? _browser;
    private IPage? _page;
    private CTRL_VarIn<int> _ctrlVarIn;

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
        await _page.GotoAsync("http://l192.168.178.121:8080", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.EvaluateAsync("() => { document.body.style.zoom = '80%'; }");
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
    public async Task TestCTRLVarInProperties()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components Dev");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_Components");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(1);
        _ctrlVarIn = area1.getElementByName<CTRL_VarIn<int>>("CoT_CTRL_VarIn1");

        // Get Text
        LocalizedText textInText = _ctrlVarIn.TextProperty;
        Assert.That(textInText.Text.Equals("This is a label"), $"Wrong initial TextInText. Text is: {textInText.Text}");
        // Set Text
        var newTextIn = new LocalizedText("en-US", "New VarIn Text");
        _ctrlVarIn.TextProperty = newTextIn;
        await _ctrlVarIn.WaitForTextProperty(newTextIn);

        // Get Value
        int value = _ctrlVarIn.ValueProperty;
        Assert.That(value.Equals(0), $"Wrong initial Value. Value is: {value}");

        // Set Value By Property
        _ctrlVarIn.ValueProperty = 3;
        await _ctrlVarIn.WaitForValueProperty(3);
        _ctrlVarIn.ValueProperty = 0;

        // Set Value By UI
        await _ctrlVarIn.VarIn.SetValueByKeyboard(5);
        await _ctrlVarIn.WaitForValueProperty(5);

        // Get ErrorState
        bool errorState = _ctrlVarIn.ErrorStateProperty;
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState. ErrorState is: {errorState}");
        // Set ErrorState
        _ctrlVarIn.ErrorStateProperty = true;

        await _ctrlVarIn.VarIn.WaitForErrorState(true);

    }


    [Test]
    public async Task TestCTRLVarInErrorProperty()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components Dev");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_Components");
        HmiPageArea area2 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(2);
        _ctrlVarIn = area2.getElementByName<CTRL_VarIn<int>>("CoT_CTRL_VarIn1");

        var min = _ctrlVarIn.MinimumProperty;
        var max = _ctrlVarIn.MaximumProperty;
        _ctrlVarIn.ValueProperty = min - 1;
        await _ctrlVarIn.VarIn.WaitForErrorState(true);

        _ctrlVarIn.ValueProperty = max + 1;
        await _ctrlVarIn.VarIn.WaitForErrorState(true);
    }

    [Test]
    public async Task TestControllErrorstate()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components Dev");
        IHmiPage _cotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_Components");
        HmiPageArea area1 = (HmiPageArea)_cotTestpage.GetAreaLayoutContentB(3);
        _ctrlVarIn = area1.getElementByName<CTRL_VarIn<int>>("CoT_CTRL_VarIn1");

        // Set ErrorState
        _ctrlVarIn.ErrorStateProperty = true;
        _ctrlVarIn.ErrorStateProperty = true;
        //controll Errorstate
        await _ctrlVarIn.VarIn.WaitForErrorState(true);
    }
}