using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using OpcPath = HmiTesting.Core.Helpers.PathHandler.OpcPath;
using Microsoft.Playwright;
using LibUA.Core;
using HmiTesting.Core.DTOs;

namespace HmiTesting.Web.Pages;

public class HmiPage : IHmiPage
{
    private IOpcUaSession _session;
    private IPage _page;
    private NodeId _pageId;
    public HmiPage(IOpcUaSession session, IPage page, NodeId pageId)

    {
        _session = session;
        _page = page;
        _pageId = pageId;
    }
    
    public IHmiPageArea GetAreaLayoutContentA(int number)
    {
        throw new NotImplementedException();
    }


    //Paths for layout Content B
    public static readonly IReadOnlyDictionary<int, OpcPath> _contentBAreas
    = Enumerable.Range(1, 3)
        .ToDictionary(
            i => i,
            i =>
                BuildPath(
                   "ScrollView",
                   "GridLayout",
                   $"Column{i}",
                   $"Area{i}")
        );

    public IHmiPageArea GetAreaLayoutContentB(int number)
    {
        OpcPath pathToArea = _contentBAreas[number];
        LocatorNodeId area = _session.ResolveNodeLocator(_page, pathToArea.ToString(), _pageId);
        return new HmiPageArea(_session, area);
    }

    public IHmiPageArea GetAreaLayoutContentC(int number)
    {
        throw new NotImplementedException();
    }

    public IHmiPageArea GetAreaLayoutListA(int number)
    {
        throw new NotImplementedException();
    }

    public IHmiPageArea GetAreaLayoutListB(int number)
    {
        throw new NotImplementedException();
    }

    public IHmiPageArea GetAreaLayoutListC(int number)
    {
        throw new NotImplementedException();
    }

    public IHmiPageArea GetAreaLayoutListD(int number)
    {
        throw new NotImplementedException();
    }
}
