namespace HmiTesting.Web.Navigation;

using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using OpcPath = HmiTesting.Core.Helpers.PathHandler.OpcPath;
using Microsoft.Playwright;
using HmiTesting.Core.DTOs;
using LibUA.Core;
using HmiTesting.Web.Pages;

public class Navigator :INavigator
{
    private IOpcUaSession _session;
    private IPage _page;

    //Optix Template Paths

        private static readonly OpcPath _navigationRoot = BuildPath(
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

    readonly OpcPath _mainPanelLoader = BuildPath(
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


    public async Task<IHmiPage> GoToPage(OpcPath pagePath,string pageName)
    {
        NodeId lastSession = _session.GetLastSession();
        OpcPath sidebarButton = _columns[1].Append(pagePath.Segments.First()).Append("Button");
        // Open Navigation
        LocatorNodeId siedebarElement = _session.ResolveNodeLocator(_page, sidebarButton.ToString(), lastSession);
        await siedebarElement.Locator.ClickAsync();

        // Navigate to the specified page
        
        for ( int i = 1; i < pagePath.Segments.Count(); i++ )
        {
            // Find the next segment in the navigation
            LocatorNodeId NavigationColumn = _session.ResolveNodeLocator(_page, _columns[i+1].ToString(), lastSession);

            string buttonLableName = pagePath.Segments[i];
           
           // Find the button with the specified label in the current navigation column
            var targetPanel = NavigationColumn.Locator.Locator($":scope > div[data-type='panel']:has(span:text(\"{buttonLableName}\"))");

            await targetPanel.Locator("div[data-type='button']").ClickAsync();
        }

        Thread.Sleep(3000); 
        LocatorNodeId actualPage = _session.ResolveNodeLocator(_page, _mainPanelLoader.Append(pageName).ToString(), lastSession);
        return new HmiPage(_session, actualPage.Locator.Page, actualPage.NodeId);
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


