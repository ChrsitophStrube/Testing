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
using System.Diagnostics;

public class PLCConnectionMissingLinks : UiTestBase
{



    [Test]
    public async Task FindUnlikedControls()
    {
        KillDataAcquisitionCore();
        //wait 20sec brefore starting the test to ensure that the DataAcquisitionCore is not running and X are visible
        Thread.Sleep(40000);
        ScreenshotOnFailureAttribute.SetPage(_page!);
        Dictionary<OpcPath, string> screens = [];
        //screens.Add(BuildPath("TestScreens", "broken PLC links"), "CoT_BrokenPLCLinks");
        screens.Add(BuildPath("TestScreens", "missing PLC links"), "CoT_MissingPLCLinks");
        //screens.Add(BuildPath("TestScreens", "Components Dev"), "CoT_Components");
        //screens = NaxigationPaser.GetScreensFromNavigationXML(@"D:\13_Masterarbeit\Repos\MAShmi\MAS-HMI\ProjectFiles\NavigationContent.xml");
        List<Exception> results = await _session.Navigator(_page).GoToAllPages(screens, RedXMissingOnElements);

        if (results.Count > 0)
        {
            Assert.Multiple(() =>
            {
                foreach (var ex in results)
                    Assert.Fail(ex.Message);
            });
        }

    }

    [Test]
    public async Task FindControlsWithDeletedPLCTag()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        Dictionary<OpcPath, string> screens = [];
        screens.Add(BuildPath("TestScreens", "broken PLC links"), "CoT_BrokenPLCLinks");
        //screens.Add(BuildPath("TestScreens", "missing PLC links"), "CoT_MissingPLCLinks");
        //screens.Add(BuildPath("TestScreens", "Components Dev"), "CoT_Components");
        //screens = NaxigationPaser.GetScreensFromNavigationXML(@"D:\13_Masterarbeit\Repos\MAShmi\MAS-HMI\ProjectFiles\NavigationContent.xml");
        List<Exception> results = await _session.Navigator(_page).GoToAllPages(screens, RedXExistingOnElements);

        if (results.Count > 0)
        {
            Assert.Multiple(() =>
            {
                foreach (var ex in results)
                    Assert.Fail(ex.Message);
            });
        }

    }

    public static void KillDataAcquisitionCore()
    {
        foreach (var p in Process.GetProcessesByName("CoreServiceHost"))
        {
            string desc;
            try { desc = p.MainModule?.FileVersionInfo?.FileDescription ?? ""; }
            catch { continue; }

            if (!desc.Equals("DataAcquisitionCore", StringComparison.OrdinalIgnoreCase))
                continue;

            bool exited = p.CloseMainWindow() && p.WaitForExit(3000);
            if (!exited && !p.HasExited)
            {
                p.Kill(entireProcessTree: true);
                p.WaitForExit(5000);
            }
            p.Dispose();
        }
    }

    public static async Task<List<Exception>> RedXExistingOnElements(IHmiPage hmiPage, string pageName, IOpcUaSession session)
    {
        return await RedXOnElements(hmiPage, pageName, session, returnElements.returnElementsWithRedX);
    }

    public static async Task<List<Exception>> RedXMissingOnElements(IHmiPage hmiPage, string pageName, IOpcUaSession session)
    {
        return await RedXOnElements(hmiPage, pageName, session, returnElements.returnElementsWithoutRedX);
    }

    public static async Task<List<Exception>> RedXOnElements(IHmiPage hmiPage, string pageName, IOpcUaSession session, returnElements returnOption)
    {
        // List of all elements with red X
        List<Exception> plcVarNotConectedExceptions = new();


        NodeId screenId = hmiPage.PageId;
        NodeId typeDefinitionId = session.GetHasTypeDefinition(screenId);
        NodeId SupertypeId = session.GetSubtypeOf(typeDefinitionId);
        string typeName = session.GetBrowsename(SupertypeId);

        IReadOnlyList<IHmiPageArea>? areas = hmiPage.GetAreasByLayout(typeName);
        if (areas == null)
        {
            return plcVarNotConectedExceptions;
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
                bool hasRedX = await FindRedXonElement(element.Locator);

                if ((returnOption == returnElements.returnElementsWithRedX && hasRedX) ||
                    (returnOption == returnElements.returnElementsWithoutRedX && !hasRedX))
                {
                    string browsename = session.GetBrowsename(element.NodeId);
                    string message = returnElements.returnElementsWithRedX == returnOption
                        ? $"red X is found on page:{pageName} on element: {browsename}"
                        : $"red X is missing on page:{pageName} on element: {browsename}";

                    plcVarNotConectedExceptions.Add(new Exception(message));
                }

            }
        }

        return plcVarNotConectedExceptions;

    }


    public enum returnElements
    {
        returnElementsWithRedX,
        returnElementsWithoutRedX
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
