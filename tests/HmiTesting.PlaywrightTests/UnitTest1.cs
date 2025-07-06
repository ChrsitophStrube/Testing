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

public class Tests
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

        //conect to OPCUA Server
        OpcUaClient client = new OpcUaClient();
        _session = (OpcUaSession)client.Connect("MyHMI_Template_Unencrypted");
    }

    [TearDown]
    public async Task DisconnectOpcUaServer()
    {
        _session?.Disconnect();
        await _page?.CloseAsync();
        await _browser?.CloseAsync();
    }

    [Test]
    public async Task TestCMPButtonBlank()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        HmiPageArea area1 = (HmiPageArea)CotTestpage.GetAreaLayoutContentB(1);
        CMPButtonBlank buttonBlank = area1.getElementByName<CMPButtonBlank>("CoT_CMP_ButtonBlank");

        // set Initial State
        NodeId buttonBlankClickToggleId = _session.GetNodeIdFromPath("ButtonBlankClickToggle", area1._area.NodeId);
        _session.SetValue<bool>(buttonBlankClickToggleId, false);

        //Click
        await buttonBlank.Click();
        bool buttonBlankClickToggle = _session.GetValue<bool>(buttonBlankClickToggleId);
        Assert.That(buttonBlankClickToggle.Equals(true), "Button Output is not True after Click");
        _session.SetValue<bool>(buttonBlankClickToggleId, false);

        //Long Click
        await buttonBlank.LongClick(2000);
        buttonBlankClickToggle = _session.GetValue<bool>(buttonBlankClickToggleId);
        Assert.That(buttonBlankClickToggle.Equals(true), "Button Output is not True after Long Click");
        _session.SetValue<bool>(buttonBlankClickToggleId, false);

        //Get IconName
        string Iconname = await buttonBlank.GetIconName();
        Assert.That(Iconname.Equals("ico-action-household_supplies.svg"), $"Wrong icon Path set to button: {Iconname}");


        //Enable 
        bool enabledState = await buttonBlank.IsEnabled();
        Assert.True(enabledState, "Button should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", buttonBlank._button.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await buttonBlank.IsEnabled();
        Assert.False(enabledState, "Button should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);


        //VisebiletyCheck
        bool visibleState = await buttonBlank.IsVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        NodeId visible = _session.GetNodeIdFromPath("visibility", buttonBlank._button.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await buttonBlank.IsVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = buttonBlank.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

    }

    [Test]
    public async Task TestCMPButtonAction()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        HmiPageArea area1 = (HmiPageArea)CotTestpage.GetAreaLayoutContentB(1);
        CMPButtonAction buttonAction = area1.getElementByName<CMPButtonAction>("CoT_CMP_ButtonAction");

        // set Initial State
        NodeId buttonActionClickToggleId = _session.GetNodeIdFromPath("ButtonActionClickToggle", area1._area.NodeId);
        _session.SetValue<bool>(buttonActionClickToggleId, false);

        //Click
        await buttonAction.Click();
        bool buttonBlankClickToggle = _session.GetValue<bool>(buttonActionClickToggleId);
        Assert.That(buttonBlankClickToggle.Equals(true), "Button Output is not True after Click");
        _session.SetValue<bool>(buttonActionClickToggleId, false);

        //Long Click
        await buttonAction.LongClick(2000);
        buttonBlankClickToggle = _session.GetValue<bool>(buttonActionClickToggleId);
        Assert.That(buttonBlankClickToggle.Equals(true), "Button Output is not True after Long Click");
        _session.SetValue<bool>(buttonActionClickToggleId, false);

        //Get IconName
        string Iconname = await buttonAction.GetIconName();
        Assert.That(Iconname.Equals("ico-action-touch.svg"), $"Wrong icon Path set to button: {Iconname}");


        //Enable 
        bool enabledState = await buttonAction.IsEnabled();
        Assert.True(enabledState, "Button should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", buttonAction._button.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await buttonAction.IsEnabled();
        Assert.False(enabledState, "Button should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);


        //VisebiletyCheck
        bool visibleState = await buttonAction.IsVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        NodeId visible = _session.GetNodeIdFromPath("visibility", buttonAction._button.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await buttonAction.IsVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = buttonAction.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

        //Get Text
        string buttonText = await  buttonAction.GetText();
        Assert.That(buttonText.Equals("Button"));
    }

    [Test]
    public async Task TestCMPButtonLed()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        HmiPageArea area1 = (HmiPageArea)CotTestpage.GetAreaLayoutContentB(1);
        CMPButtonLed buttonLed = area1.getElementByName<CMPButtonLed>("CoT_CMP_ButtonLed");

        // set Initial State
        NodeId buttonLedClickToggleId = _session.GetNodeIdFromPath("ButtonLedClickToggle", area1._area.NodeId);
        _session.SetValue<bool>(buttonLedClickToggleId, false);

        //Click
        await buttonLed.Click();
        bool buttonBlankClickToggle = _session.GetValue<bool>(buttonLedClickToggleId);
        Assert.That(buttonBlankClickToggle.Equals(true), "Button Output is not True after Click");
        _session.SetValue<bool>(buttonLedClickToggleId, false);

        //Long Click
        await buttonLed.LongClick(2000);
        buttonBlankClickToggle = _session.GetValue<bool>(buttonLedClickToggleId);
        Assert.That(buttonBlankClickToggle.Equals(true), "Button Output is not True after Long Click");
        _session.SetValue<bool>(buttonLedClickToggleId, false);

        //Get IconName
        string Iconname = await buttonLed.GetIconName();
        Assert.That(Iconname.Equals("ico-action-touch.svg"), $"Wrong icon Path set to button: {Iconname}");


        //Enable 
        bool enabledState = await buttonLed.IsEnabled();
        Assert.True(enabledState, "Button should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", buttonLed._button.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await buttonLed.IsEnabled();
        Assert.False(enabledState, "Button should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);


        //VisebiletyCheck
        bool visibleState = await buttonLed.IsVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        NodeId visible = _session.GetNodeIdFromPath("visibility", buttonLed._button.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await buttonLed.IsVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = buttonLed.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

        //Get Text
        string buttonText = await buttonLed.GetText();
        Assert.That(buttonText.Equals("Button"));

        //Get Led States
        string ledState = await buttonLed.GetLedState();
        Assert.That(buttonText.Equals("Button"));
    }

    [Test]
    public async Task TestCMPSwitch()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        var area2 = CotTestpage.GetAreaLayoutContentB(2);
        CMPSwitch cMPSwitch = area2.getElementByName<CMPSwitch>("CoT_CMP_Switch");

        // Switch switch 
        bool command = await cMPSwitch.GetCommand();
        Assert.False(command, "Initial switchState should off");
        await cMPSwitch.SetCommand(!command);
        command = await cMPSwitch.GetCommand();
        Assert.True(command, "after switching switchState should on");

        //Enable Check
        bool enabledState = await cMPSwitch.IsEnabled();
        Assert.True(enabledState, "Switch should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", cMPSwitch._switch.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await cMPSwitch.IsEnabled();
        Assert.False(enabledState, "Switch should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);

        //VisebiletyCheck
        bool visibleState = await cMPSwitch.IsVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        NodeId visible = _session.GetNodeIdFromPath("visibility", cMPSwitch._switch.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await cMPSwitch.IsVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = cMPSwitch.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

    }

    [Test]
    public async Task TestCMPCheckbox()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        var area2 = CotTestpage.GetAreaLayoutContentB(2);
        CMPCheckbox cMPCheckbox = area2.getElementByName<CMPCheckbox>("CoT_CMP_CheckBox");

        // Check checkbox 
        bool command = await cMPCheckbox.GetChecked();
        Assert.False(command, "Initial CheckBox state should be off");
        await cMPCheckbox.SetChecked(!command);
        command = await cMPCheckbox.GetChecked();
        Assert.True(command, "after click CheckBox state should on");

        //Enable Check
        bool enabledState = await cMPCheckbox.IsEnabled();
        Assert.True(enabledState, "Switch should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", cMPCheckbox._checkbox.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await cMPCheckbox.IsEnabled();
        Assert.False(enabledState, "Switch should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);


        //VisebiletyCheck
        bool visibleState = await cMPCheckbox.IsVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Checkbox
        NodeId visible = _session.GetNodeIdFromPath("visibility", cMPCheckbox._checkbox.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await cMPCheckbox.IsVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = cMPCheckbox.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

    }
    [Test]
    public async Task TestCMPRadiobutton()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        var area2 = CotTestpage.GetAreaLayoutContentB(2);
        CMPRadiobutton cMPRadiobutton = area2.getElementByName<CMPRadiobutton>("CoT_CMP_RadioButton");

        // Check/Uncheck Radiobutton
        bool command = await cMPRadiobutton.GetChecked();
        Assert.False(command, "Initial Radiobutton state should be off");
        await cMPRadiobutton.SetChecked(!command);
        command = await cMPRadiobutton.GetChecked();
        Assert.True(command, "after click Radiobutton state should on");

        //Enable Check
        bool enabledState = await cMPRadiobutton.IsEnabled();
        Assert.True(enabledState, "Radiobutton should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", cMPRadiobutton._radiobutton.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await cMPRadiobutton.IsEnabled();
        Assert.False(enabledState, "Radiobutton should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);


        //VisebiletyCheck
        bool visibleState = await cMPRadiobutton.IsVisibleAsync();
        Assert.True(visibleState, "Radiobutton should be visible initially");
        //Hide Checkbox
        NodeId visible = _session.GetNodeIdFromPath("visibility", cMPRadiobutton._radiobutton.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await cMPRadiobutton.IsVisibleAsync();
        Assert.False(visibleState, "Radiobutton should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = cMPRadiobutton.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

    }

    [Test]
    public async Task TestCMPDropdown()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        var area2 = CotTestpage.GetAreaLayoutContentB(2);
        CMPDropdown cMPDropdown = area2.getElementByName<CMPDropdown>("CoT_CMP_Dropdown");

        // Select third option in the dropdown
        string optionText = "Option 2";
        await cMPDropdown.SelectOption(optionText);
        string selectedText = await cMPDropdown.SelectedText();
        Assert.That(optionText.Equals(selectedText), $"Selected text should be '{optionText}' but is '{selectedText}'");
        int selectedId = cMPDropdown.GeSelectedId();
        Assert.That(selectedId.Equals(2), $"Selected ID should be 2 but is {selectedId}");


        //Enable Check
        bool enabledState = await cMPDropdown.IsEnabled();
        Assert.True(enabledState, "Dropdown should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", cMPDropdown._dropdown.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Dropdown is disabled
        enabledState = await cMPDropdown.IsEnabled();
        Assert.False(enabledState, "Dropdown should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);


        //VisebiletyCheck
        bool visibleState = await cMPDropdown.IsVisibleAsync();
        Assert.True(visibleState, "Radiobutton should be visible initially");
        //Hide Checkbox
        NodeId visible = _session.GetNodeIdFromPath("visibility", cMPDropdown._dropdown.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await cMPDropdown.IsVisibleAsync();
        Assert.False(visibleState, "Radiobutton should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = cMPDropdown.GetUserRole();
        Assert.That(userRole.Equals("Produce"), $"User role should be 'Produce' but is{userRole} ");

    }

    [Test]
    public async Task TestCMPVarIn()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        var area2 = CotTestpage.GetAreaLayoutContentB(2);


        // INTEGER Input Field Test
        CMPVarIn<int> cMPVarInInt = area2.getElementByName<CMPVarIn<int>>("CoT_CMP_VarIn_Int");
        // Set value in the input field
        int valueInt = 42;
        await cMPVarInInt.SetValueByKeyboard(valueInt);

        // Verify that the value was set correctly
        int inputValueInt = await cMPVarInInt.GetValue();
        Assert.That(inputValueInt.Equals(valueInt), $"Input value should be {valueInt} but is {valueInt}");

        // DOUBLE Input Field Test
        CMPVarIn<double> cMPVarInDouble = area2.getElementByName<CMPVarIn<double>>("CoT_CMP_VarIn_Double");

        //set DecimalPlaces
        NodeId decimalplaces = _session.GetNodeIdFromPath("decimalPlaces", cMPVarInDouble._varIn.NodeId);
        _session.SetValue<int>(decimalplaces, 2);

        // Set value in the input field
        double valueDouble = 12.35;
        await cMPVarInDouble.SetValueByKeyboard(valueDouble);

        // Verify that the value was set correctly
        double inputValuedouble = await cMPVarInDouble.GetValue();
        Assert.That(inputValueInt.Equals(valueInt), $"Input value should be {valueDouble} but is {valueDouble}");


        //Check Errorstate
        bool initialErrorstate = await cMPVarInInt.GetErrorState();
        Assert.That(initialErrorstate.Equals(false), " Initial error state should be false");

        //Set to errorstate By minValue
        NodeId minValue = _session.GetNodeIdFromPath("minimum", cMPVarInDouble._varIn.NodeId);
        _session.SetValue<int>(minValue, 10);
        NodeId value = _session.GetNodeIdFromPath("value", cMPVarInDouble._varIn.NodeId);
        _session.SetValue<int>(value, 5);
        bool minErrorstate = await cMPVarInDouble.GetErrorState();
        Assert.That(minErrorstate.Equals(true), "value under MinLimit-> error state should be true");

        //Set to Errorstate By maxValue
        NodeId maxValue = _session.GetNodeIdFromPath("maximum", cMPVarInDouble._varIn.NodeId);
        _session.SetValue<int>(maxValue, 5);
        _session.SetValue<int>(value, 10);
        bool maxErrorstate = await cMPVarInDouble.GetErrorState();
        Assert.That(maxErrorstate.Equals(true), "value under MinLimit-> error state should be true");



        //Enable Check
        bool enabledState = await cMPVarInInt.IsEnabled();
        Assert.True(enabledState, "Switch should be enabled initially");
        //disable 
        NodeId eneble = _session.GetNodeIdFromPath("enable", cMPVarInInt._varIn.NodeId);
        _session.SetValue<bool>(eneble, false);
        //Check if Switch is disabled
        enabledState = await cMPVarInInt.IsEnabled();
        Assert.False(enabledState, "Switch should be disabled after setting enable to false");
        //enable again
        _session.SetValue<bool>(eneble, true);

        //VisebiletyCheck
        bool visibleState = await cMPVarInInt.IsVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        NodeId visible = _session.GetNodeIdFromPath("visibility", cMPVarInInt._varIn.NodeId);
        _session.SetValue<bool>(visible, false);
        visibleState = await cMPVarInInt.IsVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        _session.SetValue<bool>(visible, true);

        //CheckUserRole
        string userRole = cMPVarInInt.GetUserRole();
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");
                                                                                          
    }

}