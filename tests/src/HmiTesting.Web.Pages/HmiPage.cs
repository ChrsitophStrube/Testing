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
    public IPage Page { get; }
    public NodeId PageId { get; }

    private readonly Dictionary<string, (IReadOnlyDictionary<int, OpcPath> Map, Func<int, IHmiPageArea> Getter)> _layoutRegistry
    = new(StringComparer.OrdinalIgnoreCase);

    public HmiPage(IOpcUaSession session, IPage page, NodeId pageId)

    {
        _session = session;
        Page = page;
        PageId = pageId;

        _layoutRegistry["CoT_ContentA"] = (_contentAAreas, GetAreaLayoutContentA);
        _layoutRegistry["CoT_ContentB"] = (_contentBAreas, GetAreaLayoutContentB);
        _layoutRegistry["CoT_ContentC"] = (_contentCAreas, GetAreaLayoutContentC);

        _layoutRegistry["CoT_ListA"] = (_contentListAAreas, GetAreaLayoutListA);
        _layoutRegistry["CoT_ListB"] = (_contentListBAreas, GetAreaLayoutListB);
        _layoutRegistry["CoT_ListC"] = (_contentListCAreas, GetAreaLayoutListC);
        _layoutRegistry["CoT_ListD"] = (_contentListDAreas, GetAreaLayoutListD);
    }

    //Paths for layout Content A
    public static readonly IReadOnlyDictionary<int, OpcPath> _contentAAreas
    = Enumerable.Range(1, 2)
        .ToDictionary(
            i => i,
            i =>
                BuildPath(
                   "ScrollView",
                   "GridLayout",
                   $"Column{i}",
                   $"Area{i}")
        );

    public IHmiPageArea GetAreaLayoutContentA(int number)
    {
        const int Min = 1;
        const int Max = 2;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentAAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
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
        const int Min = 1;
        const int Max = 3;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentBAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
    }

    //Paths for layout Content C
    public static readonly IReadOnlyDictionary<int, OpcPath> _contentCAreas
    = Enumerable.Range(1, 4)
        .ToDictionary(
            i => i,
            i =>
                BuildPath(
                   "ScrollView",
                   "GridLayout",
                   $"ScrollView1{i}",
                   $"Area{i}")
        );

    public IHmiPageArea GetAreaLayoutContentC(int number)
    {
        const int Min = 1;
        const int Max = 4;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentCAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
    }

    public static readonly IReadOnlyDictionary<int, OpcPath> _contentListAAreas =
    new Dictionary<int, OpcPath>
    {
        { 1, BuildPath("ScrollView", "GridLayout", "Area1") },
        { 2, BuildPath("ScrollView", "GridLayout", "ScrollView2", "Area2") }
    };

    public IHmiPageArea GetAreaLayoutListA(int number)
    {
        const int Min = 1;
        const int Max = 2;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentListAAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
    }

    public static readonly IReadOnlyDictionary<int, OpcPath> _contentListBAreas =
    new Dictionary<int, OpcPath>
    {
            { 1, BuildPath("ScrollView", "GridLayout", "Area1") },
            { 2, BuildPath("ScrollView", "GridLayout", "Area2") },
            { 3, BuildPath("ScrollView", "GridLayout", "ScrollView3", "Area3") }
    };
    public IHmiPageArea GetAreaLayoutListB(int number)
    {
        const int Min = 1;
        const int Max = 3;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentListBAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
    }

    public static readonly IReadOnlyDictionary<int, OpcPath> _contentListCAreas =
    new Dictionary<int, OpcPath>
    {
                { 1, BuildPath("ScrollView", "GridLayout", "Area1") },
                { 2, BuildPath("ScrollView", "GridLayout", "Area2") },
                { 3, BuildPath("ScrollView", "GridLayout", "ScrollView3", "Area3") }
    };

    public IHmiPageArea GetAreaLayoutListC(int number)
    {
        const int Min = 1;
        const int Max = 3;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentListCAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
    }

    public static readonly IReadOnlyDictionary<int, OpcPath> _contentListDAreas =
new Dictionary<int, OpcPath>
{
                { 1, BuildPath("ScrollView", "GridLayout", "ScrollView1", "Area1") },
                { 2, BuildPath("ScrollView", "GridLayout", "Area2") }
};

    public IHmiPageArea GetAreaLayoutListD(int number)
    {
        const int Min = 1;
        const int Max = 2;

        if (number < Min || number > Max)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                $"No area with this number. Area needes to be brtween {Min} and {Max}");
        OpcPath pathToArea = _contentListDAreas[number];
        LocatorNodeId area = _session.GetNodeLocator(Page, pathToArea.ToString(), PageId);
        return new HmiPageArea(_session, area);
    }

    public IReadOnlyList<IHmiPageArea>? GetAreasByLayout(string layout)
    {
        
        if (string.IsNullOrEmpty(layout)) return null;

        if (!_layoutRegistry.TryGetValue(layout, out var entry))
        {
                return null; 
        }

        var (map, getter) = entry;

        
        var areas = map.Keys
                       .OrderBy(i => i)
                       .Select(getter)
                       .ToList()
                       .AsReadOnly();

        return areas;
    }
}
