
using LibUA.Core;
using HmiTesting.Core.DTOs;
using Microsoft.Playwright;
using LibUA;

namespace HmiTesting.Core.Helpers;

public static class OpcUaHelper
{
    //static NodeIDs (fix defined in Stack will not change)
    public static readonly NodeId _objectsFolderNode = new NodeId(0, 85);
    public static readonly NodeId _namespaceIndexNode = new NodeId(0, 2255);



    public static BrowsePath CreatePath(NodeId startNode, ushort nsIndex, string path)
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


    public static NodeId GetNodeIdfromPath(this Client client, ushort nsIndex, string path, NodeId? startNode = null)
    {
        // set start node to "Objects" if not set
        if (startNode == null)
        {
            startNode = _objectsFolderNode; // Objects folder
        }

        BrowsePath[] paths = { CreatePath(startNode, nsIndex, path) };
        BrowsePathResult[] results;
        StatusCode sc = client.TranslateBrowsePathsToNodeIds(paths, out results);

        if (sc == StatusCode.Good &&
            results.Length > 0)
        {
            return results[0].Targets[0].Target;
        }
        else
        {
            throw new Exception($"can not get NodeId from Path");
        }
    }

    public static NodeId GetLastSession(this Client client,string optixProjectName, ushort nsIndex)
    {
        NodeId webEngine = GetNodeIdfromPath(client,nsIndex, $"/{optixProjectName}/UI/WebPresentationEngine");
        ushort nsSessions = GetNamespaceIndex(client,"urn:FTOptix:UI");
        NodeId sessions = GetNodeIdfromPath(client,nsSessions, "/Sessions", webEngine);
        var bd = new BrowseDescription(
                sessions,
                BrowseDirection.Forward,
                new NodeId(0, (uint)RefType.HasComponent),
                true,
                0xFFFFFFFFu,
                BrowseResultMask.All);
        BrowseResult[] results;
        StatusCode sc = client.Browse(new[] { bd },
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

    public static LocatorNodeId ResolveNodeLocator(this Client client, ushort nsIndex, IPage page, string pathToNode, NodeId startNodeId = null)
    {
        if (startNodeId == null)
        {
            startNodeId = _objectsFolderNode;
        }

        NodeId nodeId = GetNodeIdfromPath(client, nsIndex, pathToNode, startNodeId);
        ILocator locator = page.Locator($"[id='{nodeId}']");
        return new LocatorNodeId { Locator = locator, NodeId = nodeId };
    }

 
    public static ushort GetNamespaceIndex(this Client client, string namespaceUrl)
    {

        var readRes = client.Read(new ReadValueId[]
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


    public static void SetValue<T>(this Client client, NodeId nodeId, T value)
    {

        var writeVal = new WriteValue(
                nodeId,
                NodeAttribute.Value,
                null,
                new DataValue(value));

        uint[] itemStatus;
        StatusCode svc = client.Write(
                new[] { writeVal },    // WriteValue[]
                out itemStatus);

        if (svc != StatusCode.Good)
        {
            throw new Exception($"can not write value to OPC UA Server");
        }

    }

    public static T GetValue<T>(this Client client,NodeId nodeId)
    {
        var readRes = client.Read(new ReadValueId[]
            {
                    new ReadValueId(nodeId, NodeAttribute.Value, null, new QualifiedName(0, null)),

            }, out DataValue[] dvs);

        return (T)dvs[0].Value;
    }

}