using System.Runtime.CompilerServices;
using HmiTesting.Core.Interfaces;
using HmiTesting.Web.Navigation;
using LibUA;
using LibUA.Core;
using Microsoft.Playwright;
using HmiTesting.Core.Helpers;
using HmiTesting.Core.DTOs;

namespace HmiTesting.OpcUa;

public class OpcUaSession :
    IOpcUaSession
{
    private Client _client;
    private string _optixProjectName;
    private ushort _optixProjectNamespace;


    //static NodeIDs (fix defined in Stack will not change)
    public static readonly NodeId _objectsFolderNode = new NodeId(0, 85);
    public static readonly NodeId _namespaceIndexNode = new NodeId(0, 2255);


    public OpcUaSession(Client client, string optixProjectName)
    {
        _client = client;
        _optixProjectName = optixProjectName;
        _optixProjectNamespace = client.GetNamespaceIndex(_optixProjectName);
    }

    public INavigator Navigator(IPage MainPageObject)
    {
        return new Navigator(this, MainPageObject);
    }

    public IHmiPage GetActualPage()
    {
        throw new NotImplementedException();
    }

    public IHmiPage GetHeader()
    {
        throw new NotImplementedException();
    }

    public NodeId GetNodeIdFromPath(string path, NodeId? startNode = null)
    {
        return GetNodeIdFromPath(_optixProjectNamespace, path, startNode);
    }

    public NodeId GetNodeIdFromPath(ushort nsIndex, string path, NodeId startNode = null)
    {
        // set start node to "Objects" if not set
        if (startNode == null)
        {
            startNode = _objectsFolderNode; // Objects folder
        }

        BrowsePath[] paths = { CreatePath(startNode, nsIndex, path) };
        BrowsePathResult[] results;
        StatusCode sc = _client.TranslateBrowsePathsToNodeIds(paths, out results);

        if (sc == StatusCode.Good &&
            results.Length > 0
            && results[0].Targets.Length > 0)
        {
            return results[0].Targets[0].Target;
        }
        else
        {
            throw new Exception($"can not get NodeId from Path:{path}");
        }
    }

    private BrowsePath CreatePath(NodeId startNode, ushort nsIndex, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("path should not be empty", nameof(path));

        string[] segments = path.Trim('/')
                                 .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        var rpe = segments.Select(name =>
                       new RelativePathElement(
                               new NodeId(0, 33),      // ReferenceTypeId
                               false,                  // IsInverse
                               true,                   // IncludeSubtypes
                               new QualifiedName(nsIndex, name)))
                          .ToArray();

        return new BrowsePath(startNode, rpe);
    }

    public List<NodeId> GetChildren(NodeId parentNodeId)
    {

        var bd = new BrowseDescription(
                parentNodeId,
                BrowseDirection.Forward,
                new NodeId(0, (uint)RefType.HasComponent),
                true,
                0xFFFFFFFFu,
                BrowseResultMask.All);
        BrowseResult[] results;
        StatusCode sc = _client.Browse(new[] { bd },
                                      1000,
                                      out results);
        if (sc == StatusCode.Good &&
            results.Length > 0)
        {
            return results[0]
                .Refs
                .Select(r => r.TargetId)
                .ToList();
        }
        else
        {
            throw new Exception($"can not get Children");
        }

    }



    public NodeId GetLastSession()
    {
        NodeId webEngine = GetNodeIdFromPath(_optixProjectNamespace, $"/{_optixProjectName}/UI/WebPresentationEngine");
        ushort nsSessions = GetNamespaceIndex("urn:FTOptix:UI");
        NodeId sessions = GetNodeIdFromPath(nsSessions, "/Sessions", webEngine);
        var bd = new BrowseDescription(
                sessions,
                BrowseDirection.Forward,
                new NodeId(0, (uint)RefType.HasComponent),
                true,
                0xFFFFFFFFu,
                BrowseResultMask.All);
        BrowseResult[] results;
        StatusCode sc = _client.Browse(new[] { bd },
                                      1000,
                                      out results);
        if (sc == StatusCode.Good &&
            results.Length > 0)
        {
            return results[0].Refs
                   .Where(r => r.BrowseName.Name != "Session1")
                   .First().TargetId;
        }
        else
        {
            throw new Exception($"can not get Websession");
        }

    }


    public ushort GetProjectNamespaceIndex()
    {
        return GetNamespaceIndex(_optixProjectName);
    }
    public ushort GetNamespaceIndex(string namespaceUrl)
    {

        var readRes = _client.Read(new ReadValueId[]
                {
                    new ReadValueId(_namespaceIndexNode , NodeAttribute.Value, null, new QualifiedName(0, null)),
                }, out DataValue[] dvs);

        if (readRes != StatusCode.Good)
        {
            throw new Exception($"can not read Namespaceindexes");
        }

        string[] nsArray = (string[])dvs[0].Value;
        ushort index = (ushort)Array.IndexOf(nsArray, namespaceUrl);
        return index;
    }


    public void SetValue<T>(NodeId nodeId, T value)
    {

        var writeVal = new WriteValue(
                nodeId,
                NodeAttribute.Value,
                null,
                new DataValue(value));

        uint[] itemStatus;
        StatusCode svc = _client.Write(
                new[] { writeVal },    // WriteValue[]
                out itemStatus);

        if (svc != StatusCode.Good)
        {
            throw new Exception($"can not write value to OPC UA Server");
        }

    }

    public T GetValue<T>(NodeId nodeId)
    {
        var readRes = _client.Read(new ReadValueId[]
            {
                    new ReadValueId(nodeId, NodeAttribute.Value, null, new QualifiedName(0, null)),

            }, out DataValue[] dvs);

        return (T)dvs[0].Value;
    }

    public string GetBrowsename(NodeId nodeId)
    {
        var readRes = _client.Read(new ReadValueId[]
            {
                    new ReadValueId(nodeId, NodeAttribute.BrowseName, null, new QualifiedName(0, null)),

            }, out DataValue[] dvs);

        //return ((QualifiedName)dvs[0]?.Value).Name;
        return dvs is { Length: > 0 }
             && dvs[0]?.Value is QualifiedName qn
                ? qn.Name
                : null;

    }

    public LocatorNodeId ResolveNodeLocator(IPage page, string pathToNode, NodeId startNodeId = null)
    {
        if (startNodeId == null)
        {
            startNodeId = _objectsFolderNode;
        }

        NodeId nodeId = GetNodeIdFromPath(_optixProjectNamespace, pathToNode, startNodeId);
        ILocator locator = page.Locator($"[id='{nodeId}']");
        return new LocatorNodeId { Locator = locator, NodeId = nodeId };
    }

    public void Disconnect()
    {
        if (_client != null)
        {
            _client.CloseSession();
            _client.Disconnect();
            _client.Dispose();
        }
    }

}

