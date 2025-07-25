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
        await _page.GotoAsync("http://localhost:8080", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.EvaluateAsync("() => { document.body.style.zoom = '80%'; }");
        await _page.WaitForTimeoutAsync(2000);

        ScreenshotOnFailureAttribute.SetPage(_page!);

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
        buttonBlank.EnableProperty = false;
        //Check if Switch is disabled
        enabledState = await buttonBlank.IsEnabled();
        Assert.False(enabledState, "Button should be disabled after setting enable to false");
        //enable again
        buttonBlank.EnableProperty = true;


        //VisebiletyCheck
        await buttonBlank.WaitForVisibleAsync(true);
        bool visibleState = await buttonBlank.GetVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        buttonBlank.VisisbleProperty = false;
        await buttonBlank.WaitForVisibleAsync(false);
        visibleState = await buttonBlank.GetVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        buttonBlank.VisisbleProperty = true;

        //CheckUserRole
        string userRole = buttonBlank.UserRoleProperty;
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
        await buttonAction.WaitForEnablePropertyAsync(true);
        bool enabledState = await buttonAction.IsEnabled();
        Assert.True(enabledState, "Button should be enabled initially");
        //disable 
        buttonAction.EnableProperty = false;
        //Check if Switch is disabled
        await buttonAction.WaitForEnablePropertyAsync(false);
        enabledState = await buttonAction.IsEnabled();
        Assert.False(enabledState, "Button should be disabled after setting enable to false");
        //enable again
        buttonAction.EnableProperty = true;


        //VisebiletyCheck
        await buttonAction.WaitForVisibleAsync(true);
        bool visibleState = await buttonAction.GetVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        buttonAction.VisisbleProperty = false;
        await buttonAction.WaitForVisibleAsync(false);
        visibleState = await buttonAction.GetVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        buttonAction.VisisbleProperty = true;

        //CheckUserRole
        string userRole = buttonAction.UserRoleProperty;
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

        //Get Text
        string buttonText = await buttonAction.GetText();
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
        buttonLed.EnableProperty = false;
        //Check if Switch is disabled
        enabledState = await buttonLed.IsEnabled();
        Assert.False(enabledState, "Button should be disabled after setting enable to false");
        //enable again
        buttonLed.EnableProperty = true;


        //VisebiletyCheck
        await buttonLed.WaitForVisibleAsync(true);
        bool visibleState = await buttonLed.GetVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        buttonLed.VisisbleProperty = false;
        await buttonLed.WaitForVisibleAsync(false);
        visibleState = await buttonLed.GetVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        buttonLed.VisisbleProperty = true;

        //CheckUserRole
        string userRole = buttonLed.UserRoleProperty;
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
        cMPSwitch.EnableProperty = false;
        //Check if Switch is disabled
        await cMPSwitch.WaitForEnablePropertyAsync(false);
        enabledState = await cMPSwitch.IsEnabled();
        Assert.False(enabledState, "Switch should be disabled after setting enable to false");
        //enable again
        cMPSwitch.EnableProperty = true;
        await cMPSwitch.WaitForEnablePropertyAsync(true);

        //VisebiletyCheck
        await cMPSwitch.WaitForVisibleAsync(true);
        bool visibleState = await cMPSwitch.GetVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");
        //Hide Switch
        cMPSwitch.VisisbleProperty = false;

        await cMPSwitch.WaitForVisibleAsync(false);
        visibleState = await cMPSwitch.GetVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        cMPSwitch.VisisbleProperty = true;

        //CheckUserRole
        string userRole = cMPSwitch.UserRoleProperty;
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
        cMPCheckbox.EnableProperty = false;
        //Check if Switch is disabled
        enabledState = await cMPCheckbox.IsEnabled();
        Assert.False(enabledState, "Switch should be disabled after setting enable to false");
        //enable again
        cMPCheckbox.EnableProperty = true;

        //VisebiletyCheck
        await cMPCheckbox.WaitForVisibleAsync(true);
        bool visibleState = await cMPCheckbox.GetVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");

        //Hide Checkbox
        cMPCheckbox.VisisbleProperty = false;
        await cMPCheckbox.WaitForVisibleAsync(false);
        visibleState = await cMPCheckbox.GetVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");
        //set back visible
        cMPCheckbox.VisisbleProperty = true;

        //CheckUserRole
        string userRole = cMPCheckbox.UserRoleProperty;
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
        cMPRadiobutton.EnableProperty = false;
        //Check if Switch is disabled
        enabledState = await cMPRadiobutton.IsEnabled();
        Assert.False(enabledState, "Radiobutton should be disabled after setting enable to false");
        //enable again
        cMPRadiobutton.EnableProperty = true;


        //VisebiletyCheck
        await cMPRadiobutton.WaitForVisibleAsync(true);
        bool visibleState = await cMPRadiobutton.GetVisibleAsync();
        Assert.True(visibleState, "Radiobutton should be visible initially");
        //Hide Checkbox
        cMPRadiobutton.VisisbleProperty = false;
        await cMPRadiobutton.WaitForVisibleAsync(false);
        visibleState = await cMPRadiobutton.GetVisibleAsync();
        Assert.False(visibleState, "Radiobutton should be hidden after setting visible to false");
        //set back visible
        cMPRadiobutton.VisisbleProperty = true;

        //CheckUserRole
        string userRole = cMPRadiobutton.UserRoleProperty;
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
        cMPDropdown.EnableProperty = false;
        //Check if Dropdown is disabled
        enabledState = await cMPDropdown.IsEnabled();
        Assert.False(enabledState, "Dropdown should be disabled after setting enable to false");
        //enable again
        cMPDropdown.EnableProperty = true;


        //VisebiletyCheck
        await cMPDropdown.WaitForVisibleAsync(true);
        bool visibleState = await cMPDropdown.GetVisibleAsync();
        Assert.True(visibleState, "Radiobutton should be visible initially");
        //Hide Checkbox
        cMPDropdown.VisisbleProperty = false;
        await cMPDropdown.WaitForVisibleAsync(false);
        visibleState = await cMPDropdown.GetVisibleAsync();
        Assert.False(visibleState, "Radiobutton should be hidden after setting visible to false");
        //set back visible
        cMPDropdown.VisisbleProperty = true;

        //CheckUserRole
        string userRole = cMPDropdown.UserRoleProperty;
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
        cMPVarInDouble.DecimalPlacesProperty = 2;

        // Set value in the input field
        double valueDouble = 12.35;
        await cMPVarInDouble.SetValueByKeyboard(valueDouble);

        // Verify that the value was set correctly
        double inputValuedouble = await cMPVarInDouble.GetValue();
        Assert.That(inputValueInt.Equals(valueInt), $"Input value should be {valueDouble} but is {valueDouble}");

        await cMPVarInDouble.WaitForValue(valueDouble);


        //Check Errorstate
        bool initialErrorstate = await cMPVarInInt.GetErrorState();
        Assert.That(initialErrorstate.Equals(false), " Initial error state should be false");
        await cMPVarInDouble.WaitForErrorState(false);

        //Set to errorstate By minValue
        cMPVarInDouble.MinValueProperty = 10;

        NodeId value = _session.GetNodeIdFromPath("value", cMPVarInDouble._varIn.NodeId);
        _session.SetValue<int>(value, 5);

        bool minErrorstate = await cMPVarInDouble.GetErrorState();
        Assert.That(minErrorstate.Equals(true), "value under MinLimit-> error state should be true");

        //Set to Errorstate By maxValue
        cMPVarInDouble.MaxValueProperty = 5;
        _session.SetValue<int>(value, 10);
        await cMPVarInDouble.WaitForErrorState(true);
        bool maxErrorstate = await cMPVarInDouble.GetErrorState();
        Assert.That(maxErrorstate.Equals(true), "value under MinLimit-> error state should be true");

        //Enable Check
        await cMPVarInInt.WaitForEnabled(true);
        bool enabledState = await cMPVarInInt.IsEnabled();
        Assert.True(enabledState, "VarIn should be enabled initially");
        //disable 
        cMPVarInInt.EnableProperty = false;
        //Check if Varin is disabled
        await cMPVarInInt.WaitForEnabled(false);
        enabledState = await cMPVarInInt.IsEnabled();
        Assert.False(enabledState, "VarIn should be disabled after setting enable to false");
        //enable again
        cMPVarInDouble.EnableProperty = true;

        //VisebiletyCheck
        await cMPVarInDouble.VisisblePropertyAsync(true);
        //Hide 
        cMPVarInDouble.VisisbleProperty = false;
        await cMPVarInDouble.VisisblePropertyAsync(false);

        //set back visible
        cMPVarInDouble.VisisbleProperty = true;

        //CheckUserRole
        string userRole = cMPVarInInt.UserRoleProperty;
        Assert.AreEqual("Produce", userRole, $"User role should be 'Produce' but is{userRole} ");

    }

    [Test]
    public async Task TestCMPTextIn()
    {
        var componentsDevpage = BuildPath("TestScreens", "Components1");
        var CotTestpage = await _session.Navigator(_page).GoToPage(componentsDevpage, "CoT_ComponentsOverview1");
        var area2 = CotTestpage.GetAreaLayoutContentB(2);


        // Input Field Test
        CMPTextIn cMPTextIn = area2.getElementByName<CMPTextIn>("CoT_CMP_TextIn");
        // Set value in the input field
        string value = "Honigkuchen";
        await cMPTextIn.SetValueByKeyboard(value);

        // Verify that the value was set correctly by waiting
        await cMPTextIn.WaitForInput(value);

        string readValue = await cMPTextIn.GetInput();
        Assert.That(readValue.Equals(value), $"Read value {readValue}is not matching {value}");

        //Check Errorstate False
        bool Errorstate = await cMPTextIn.GetErrorState();
        Assert.That(Errorstate.Equals(false), " Initial error state should be false");
        await cMPTextIn.WaitForErrorState(false);

        //Set Errorstate True 
        cMPTextIn.ErrorStateProperty = true;

        //Check Errorstate true
        Errorstate = await cMPTextIn.GetErrorState();
        Assert.That(Errorstate.Equals(true), "error state should be fullfilled");
        await cMPTextIn.WaitForErrorState(true);

        //Set to errorstate False
        cMPTextIn.ErrorStateProperty = false;

        //Enable Check
        bool enabledState = await cMPTextIn.IsEnabled();
        Assert.True(enabledState, "Switch should be enabled initially");

        await cMPTextIn.WaitForEnabled(true);

        //disable 
        cMPTextIn.EnableProperty = false;

        //Check if Switch is disabled
        enabledState = await cMPTextIn.IsEnabled();
        Assert.False(enabledState, "Switch should be disabled after setting enable to false");
        await cMPTextIn.WaitForEnabled(false);

        //enable again
        cMPTextIn.EnableProperty = true;

        //VisebiletyCheck
        await cMPTextIn.WaitForVisibleAsync(true);
        bool visibleState = await cMPTextIn.GetVisibleAsync();
        Assert.True(visibleState, "Switch should be visible initially");

        //Hide
        NodeId visible = _session.GetNodeIdFromPath("visibility", cMPTextIn._textIn.NodeId);
        _session.SetValue<bool>(visible, false);
        cMPTextIn.VisisbleProperty = false;
        await cMPTextIn.WaitForVisibleAsync(false);
        visibleState = await cMPTextIn.GetVisibleAsync();
        Assert.False(visibleState, "Switch should be hidden after setting visible to false");

        //set back visible
        cMPTextIn.VisisbleProperty = true;
    }

}