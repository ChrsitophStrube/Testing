namespace HmiTesting.Web.Navigation;

using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using OpcPath = HmiTesting.Core.Helpers.PathHandler.OpcPath;
using Microsoft.Playwright;
using HmiTesting.Core.DTOs;
using LibUA.Core;
using HmiTesting.Web.Pages;


public class Navigator : INavigator
{
    private IOpcUaSession _session;
    private IPage _page;

    //Optix Template Paths

    public static readonly OpcPath _navigationRoot = BuildPath(
        "UIRoot",
        "MainFrame",
        "ContentArea",
        "Navigation"
    );

    public static readonly IReadOnlyDictionary<int, OpcPath> _columns
        = Enumerable.Range(1, 5)
            .ToDictionary(
                i => i,
                i => i == 1
                    ? _navigationRoot.Append(
                        "SideBar",
                        "VerticalLayout",
                        $"Column{i}",
                        "ScrollLayout",
                        "Layout")
                    : _navigationRoot.Append(
                        "ColumnLayout",
                        $"Column{i}",
                        "ScrollLayout",
                        "Layout")
            );

    public readonly OpcPath _mainPanelLoader = BuildPath(
            "UIRoot",
            "MainFrame",
            "ContentArea",
            "ScreenViewer"
        );

    public Navigator(IOpcUaSession session, IPage page)
    {
        _session = session;
        _page = page;
    }

    public async Task<IHmiPage> GoToPage(OpcPath pagePath, string pageName, bool bypassRestriction = false)
    {
        NodeId lastSession = _session.GetLastSession();

        // Navigate to the specified page
        string NavigationElementName = "";
        for (int i = 0; i < pagePath.Segments.Count(); i++)
        {
            NavigationElementName += pagePath.Segments[i];

            OpcPath sidebarButton = _columns[i + 1].Append(NavigationElementName).Append("Button");

            //Make Navigarion Button Visible 
            if (bypassRestriction)
            {
                OpcPath navigationElement = _columns[i + 1].Append(NavigationElementName);
                NodeId navigationElementId = _session.GetNodeIdFromPath(navigationElement.ToString(), lastSession);
                NodeId visebilety = _session.GetNodeIdFromPath("urn:FTOptix:UI", "Visible", navigationElementId);
                _session.SetValue<bool>(visebilety, true);

            }

            if (i == 0)
            {
                // Click First Sidbar element to open (unindependent from color)
                LocatorNodeId siedebarElement = _session.WaitForNodeLocator(_page, sidebarButton.ToString(), lastSession);
                await siedebarElement.Locator.ClickAsync();
            }
            else
            {

                //Skip click if already selected
                OpcPath isOpenPath = _columns[i + 1].Append(NavigationElementName).Append("isOpen");
                NodeId isOpenProp  = _session.GetNodeIdFromPath(isOpenPath.ToString(), lastSession);
                bool isOpen        =  _session.GetValue<bool>(isOpenProp);

                if (!isOpen)
                {
                    // Click SidebarElement
                    LocatorNodeId siedebarElement = _session.WaitForNodeLocator(_page, sidebarButton.ToString(), lastSession);
                    await siedebarElement.Locator.ClickAsync();
                }
            }

            NavigationElementName += @" &/ ";
        }
        Thread.Sleep(3500);
        LocatorNodeId actualPage = _session.WaitForNodeLocator(_page, _mainPanelLoader.Append(pageName).ToString(), lastSession);
        return new HmiPage(_session, actualPage.Locator.Page, actualPage.NodeId);
    }

    public async Task<IReadOnlyList<Exception>> GoToAllPages(Dictionary<OpcPath, string> screens, Func<IHmiPage, string, IOpcUaSession, Task> perPage, bool bypassRestriction = true)
    {
        var errors = new List<Exception>();

        foreach (var (path, name) in screens)
        {
            try
            {
                var page = await GoToPage(path, name, bypassRestriction);
                await perPage(page, name, _session);
            }
            catch (Exception ex)
            {
                errors.Add(new Exception($"Fault loading Page '{name}' ({path})", ex));
            }
        }

        return errors;
    }



    public INavigator ReturnToNextPage()
    {
        throw new NotImplementedException();
    }

    public INavigator ReturnToPreviousPage()
    {
        throw new NotImplementedException();
    }


}


