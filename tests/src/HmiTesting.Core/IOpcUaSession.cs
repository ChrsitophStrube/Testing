
using LibUA.Core;
using HmiTesting.Core.Interfaces;
using HmiTesting.Core.DTOs;
using Microsoft.Playwright;


public interface IOpcUaSession
{

    ushort GetNamespaceIndex(string namespaceUrl);
    ushort GetProjectNamespaceIndex();
    NodeId GetNodeIdFromPath(string path, NodeId? startNode = null);
    NodeId GetNodeIdFromPath(string nsIndex, string path, NodeId startNode = null);
    NodeId WaitForNodeIdFromPath(string path, NodeId? startNode = null, TimeSpan? timeout = null, TimeSpan? pollInterval = null);
    NodeId WaitForNodeIdFromPath(string nsIndex, string path, NodeId startNode = null, TimeSpan? timeout = null, TimeSpan? pollInterval = null);
    NodeId WaitForNodeIdFromPath(ushort nsIndex, string path, NodeId startNode = null, TimeSpan? timeout = null, TimeSpan? pollInterval = null);
    NodeId GetLastSession();
    IHmiPage GetHeader();
    IHmiPage GetActualPage();
    INavigator Navigator(IPage MainPageObject);
    void SetValue<T>(NodeId nodeId, T value);
    T GetValue<T>(NodeId nodeId);
    Task<T> WaitForValueAsync<T>(NodeId nodeId, T expectedValue, int timeoutMs = 500);
    string GetBrowsename(NodeId nodeId);
    List<NodeId> GetChildren(NodeId parentNodeId);
    LocatorNodeId GetNodeLocator(IPage page, string pathToNode, NodeId startNodeId = null);
    LocatorNodeId WaitForNodeLocator(IPage page, string pathToNode, NodeId startNodeId = null);
}
