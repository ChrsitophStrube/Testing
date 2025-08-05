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

public class CMPTestsOutput
{

    private IPlaywright? playwright = null;
    private IBrowser? _browser;
    private IPage? _page;

    private OpcUaSession _session;

    [SetUp]
    public async Task Setup()
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
        ScreenshotOnFailureAttribute.SetPage(_page!);

        //conect to OPCUA Server
        OpcUaClient client = new OpcUaClient();
        _session = (OpcUaSession)client.Connect("MyHMI_Template_Unencrypted", "192.168.1.200", 59100);
    }

    [TearDown]
    public async Task DisconnectOpcUaServer()
    {
        _session?.Disconnect();
        await _page?.CloseAsync();
        await _browser?.CloseAsync();
    }

    [Test]
    public async Task TestCMPLabel()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components2");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview2");
        HmiPageArea area1 = (HmiPageArea)CotTestpage.GetAreaLayoutContentB(1);
        CMPLabel label = area1.getElementByName<CMPLabel>("CoT_CMP_Label");


        //Get IconName
        string Iconname = label.IconProperty;
        Assert.That(Iconname.Equals("ico-action-settings.svg"), $"Wrong icon Path set to button: {Iconname}");

        //Get Text
        string text = await label.GetText();
        Assert.That(text.Equals("This is a label"), $"Wrong text initial text for Label. Text is: {text}");

        //Set Text
        LocalizedText localizedNewText = new("en-US", "New Text");
        label.TextProperty = localizedNewText;
        await label.WaitForTextProperty(localizedNewText);
        await label.WaitForText("New Text");

        //VisebiletyCheck
        await label.WaitForVisibleAsync(true);
        bool visibleState = await label.GetVisibleAsync();
        Assert.True(visibleState, "Label should be visible initially");
        //Hide Switch
        label.VisisbleProperty = false;
        await label.WaitForVisibleAsync(false);
        visibleState = await label.GetVisibleAsync();
        Assert.False(visibleState, "Label should be hidden after setting visible to false");
        //set back visible
        label.VisisbleProperty = true;

    }

    [Test]
    public async Task TestCMPVarOut()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components2");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview2");
        HmiPageArea area1 = (HmiPageArea)CotTestpage.GetAreaLayoutContentB(1);
        CMPVarOut<int> varOut = area1.getElementByName<CMPVarOut<int>>("CoT_CMP_VarOut");

        //Get unit Property
        string unit = varOut.UnitProperty;
        Assert.That(unit.Equals("unit"), $"Wrong unit Path set to VarOut: {unit}");

        //Get decimalPlaces Property
        int decimalPlaces = varOut.DecimalPlacesProperty;
        Assert.That(decimalPlaces.Equals(0), $"Wrong decimalPlaces set to VarOut: {decimalPlaces}");

        //Get Value
        varOut.ValueProperty = 42;
        await varOut.WaitForValueProperty(42);
        int value = await varOut.GetValue();
        Assert.That(value.Equals(varOut.ValueProperty), $"Wrong initial value for VarOut. Value is: {value}");

        //Get ErrorState
        await varOut.WaitForErrorState(false);
        bool errorState = await varOut.GetErrorState();
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState for VarOut. ErrorState is: {errorState}");
        //Set ErrorState
        varOut.ErrorStateProperty = true;
        await varOut.WaitForErrorState(true);
        await varOut.WaitForErrorStateProperty(true);
        errorState = await varOut.GetErrorState();
        Assert.That(errorState.Equals(true), $"Wrong ErrorState for VarOut. ErrorState is: {errorState}");

        //VisebiletyCheck
        await varOut.WaitForVisibleAsync(true);
        bool visibleState = await varOut.GetVisibleAsync();
        Assert.True(visibleState, "Label should be visible initially");
        //Hide 
        varOut.VisisbleProperty = false;
        await varOut.WaitForVisibleAsync(false);
        visibleState = await varOut.GetVisibleAsync();
        Assert.False(visibleState, "Label should be hidden after setting visible to false");
        //set back visible
        varOut.VisisbleProperty = true;
    }

    [Test]
    public async Task TestCMPTextOut()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components2");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview2");
        HmiPageArea area1 = (HmiPageArea)CotTestpage.GetAreaLayoutContentB(1);
        CMPTextOut textOut = area1.getElementByName<CMPTextOut>("CoT_CMP_TextOut");

        //Get unit Property
        string unit = textOut.UnitProperty;
        Assert.That(unit.Equals("unit"), $"Wrong unit Path set to TextOut: {unit}");


        //Get Value
        LocalizedText localizedText = new("en-US", "Honigkuchen");
        textOut.TextProperty = localizedText;
        string text = await textOut.GetText();
        Assert.That(text.Equals(textOut.TextProperty.Text), $"Wrong initial value for TextOut. Value is: {text}");

        //Get ErrorState
        await textOut.WaitForErrorState(false);
        bool errorState = await textOut.GetErrorState();
        Assert.That(errorState.Equals(false), $"Wrong initial ErrorState for VarOut. ErrorState is: {errorState}");
        //Set ErrorState
        textOut.ErrorStateProperty = true;
        await textOut.WaitForErrorState(true);
        await textOut.WaitForErrorStateProperty(true);
        errorState = await textOut.GetErrorState();
        Assert.That(errorState.Equals(true), $"Wrong ErrorState for TextOut. ErrorState is: {errorState}");

        //VisebiletyCheck
        await textOut.WaitForVisibleAsync(true);
        bool visibleState = await textOut.GetVisibleAsync();
        Assert.True(visibleState, "should be visible initially");
        //Hide 
        textOut.VisisbleProperty = false;
        await textOut.WaitForVisibleAsync(false);
        visibleState = await textOut.GetVisibleAsync();
        Assert.False(visibleState, " should be hidden after setting visible to false");
        //set back visible
        textOut.VisisbleProperty = true;
    }
}