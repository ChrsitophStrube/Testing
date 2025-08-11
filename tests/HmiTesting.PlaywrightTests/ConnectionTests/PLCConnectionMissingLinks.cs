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
using HmiTesting.Core.DTOs;


public class PLCConnectionMissingLinks
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
        await _page.GotoAsync(ProjectConfig.Current.ProjectUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle
        });
        await _page.EvaluateAsync("() => { document.body.style.zoom = '90%'; }");
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

    [Test]
    public async Task FindUnlikedControls()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        Dictionary<OpcPath, string> screens = [];
       // screens.Add(BuildPath("Machine settings", "MODX1", "Belt", "General"), "DemoModX_MS_Belt_General");
       // screens.Add(BuildPath("Administer", "Device server", "Runtime"), "CoTD_Server_Runtime");
       // screens.Add(BuildPath("Administer", "SSI"), "CoTD_SSI");
        screens.Add(BuildPath("TestScreens", "broken PLC links"), "CoT_BrokenPLCLinks");
        //screens = NaxigationPaser.GetScreensFromNavigationXML(@"D:\13_Masterarbeit\Repos\MAShmi\MAS-HMI\ProjectFiles\NavigationContent.xml");



        // IHmiPage _motorGeneralPage = await _session.Navigator(_page).GoToPage(componentsDevpage, "DemoModX_MS_Belt_General",true);
        var results = await _session.Navigator(_page).GoToAllPages(screens, FindRedXOnElements);

    }

    public static async Task FindRedXOnElements(IHmiPage hmiPage, string pageName, IOpcUaSession session)
    {
        // List of all elements with red X
        List < (string pageName , string elementName)> redXElements = new();

        
        NodeId screenId = hmiPage.PageId;
        NodeId typeDefinitionId = session.GetHasTypeDefinition(screenId);
        NodeId SupertypeId = session.GetSubtypeOf(typeDefinitionId);
        string typeName = session.GetBrowsename(SupertypeId);

        IReadOnlyList<IHmiPageArea>? areas = hmiPage.GetAreasByLayout(typeName);
        if (areas == null)
        {
            return;
        }

        foreach (IHmiPageArea area in areas)
        {
            var elementsResult = area.getAllElements();
            if (elementsResult == null)
            {
                continue;
            }
            List<LocatorNodeId> elements = elementsResult;

            foreach (LocatorNodeId element in elements)
            {

                // Check if the element has a red X
                if(await FindRedXonElement(element.Locator))
                {
                    // If it has a red X, add it to the list
                    string browsename = session.GetBrowsename(element.NodeId);
                    redXElements.Add((pageName, browsename));
                }
                
            }
        }

    }

    public static async Task<bool> FindRedXonElement(ILocator element)
    {
        // Falls der Locator nichts matched, direkt false
        var handle = await element.ElementHandleAsync();
        if (handle is null) return false;

        // Im Kontext des Elements auswerten
        return await handle.EvaluateAsync<bool>(@"(root) => {
        const svgs = root.querySelectorAll('svg');
        const getStroke = (el) => {
            // computed style (robust gegen CSS)
            const cs = window.getComputedStyle(el).stroke || '';
            if (cs && cs !== 'none') return cs.trim().toLowerCase();

            // Fallback: Inline-Attribute
            const attr = (el.getAttribute('stroke') || '').trim().toLowerCase();
            if (attr) return attr;

            // Fallback: style-Attribut parsen
            const style = (el.getAttribute('style') || '').toLowerCase();
            const m = style.match(/stroke:\s*([^;]+)/);
            return m ? m[1].trim() : '';
        };

        const isRed = (color) => {
            const c = color.replace(/\s/g,'');
            return c === 'rgb(255,0,0)' || c === '#ff0000' || c === 'red';
        };

        for (const svg of svgs) {
            const lines = svg.querySelectorAll('line');
            if (lines.length !== 2) continue;

            const c1 = getStroke(lines[0]);
            const c2 = getStroke(lines[1]);

            if (isRed(c1) && isRed(c2)) {
                return true;
            }
        }
        return false;
    }");
    }

}
