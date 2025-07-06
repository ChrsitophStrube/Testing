
using LibUA.Core;
using HmiTesting.Core.Interfaces;
using HmiTesting.Core.DTOs;
using Microsoft.Playwright;


public interface IOpcUaSession
{
    
    ushort GetNamespaceIndex(string namespaceUrl);
    ushort GetProjectNamespaceIndex();
    NodeId GetNodeIdFromPath(string path, NodeId? startNode = null);
    NodeId GetLastSession();
    IHmiPage GetHeader();
    IHmiPage GetActualPage();
    INavigator Navigator(IPage MainPageObject);
    void SetValue<T>(NodeId nodeId, T value);
    T GetValue<T>(NodeId nodeId);
    string GetBrowsename(NodeId nodeId);
    List<NodeId> GetChildren(NodeId parentNodeId);
    LocatorNodeId ResolveNodeLocator(IPage page, string pathToNode, NodeId startNodeId = null);
}
