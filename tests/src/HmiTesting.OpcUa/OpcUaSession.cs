using System.Runtime.CompilerServices;
using HmiTesting.Core.Interfaces;
using HmiTesting.Web.Navigation;
using LibUA;
using LibUA.Core;
using Microsoft.Playwright;
using HmiTesting.Core.Helpers;
using HmiTesting.Core.DTOs;
using System.Diagnostics;

namespace HmiTesting.OpcUa;

public class OpcUaSession :
    IOpcUaSession
{
    private readonly NotifyingClient _client;
    private string _optixProjectName;
    private ushort _optixProjectNamespace;


    //static NodeIDs (fix defined in Stack will not change)
    public static readonly NodeId _objectsFolderNode = new NodeId(0, 85);
    public static readonly NodeId _namespaceIndexNode = new NodeId(0, 2255);


    public OpcUaSession(NotifyingClient client, string optixProjectName)
    {
        _client = client;
        _optixProjectName = optixProjectName;
        _optixProjectNamespace = client.GetNamespaceIndex(_optixProjectName);
        _client.DataChange += HandleDataChange;   // <<< Event‑Abo

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

    public NodeId GetNodeIdFromPath(string nsIndex, string path, NodeId startNode = null)
    {
        ushort nsIndexInt = GetNamespaceIndex(nsIndex);
        return GetNodeIdFromPath(nsIndexInt, path, startNode);

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

    public async Task<T> WaitForValueAsync<T>(NodeId nodeId, T expectedValue, int timeoutMs = 500)
    {
        var interval = TimeSpan.FromMilliseconds(50);     // Poll-Intervall
        var start = Stopwatch.GetTimestamp();
        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMs);
        double timeoutTicks = timeout.TotalSeconds * Stopwatch.Frequency;

        while (true)
        {
            T current = GetValue<T>(nodeId);

            // Compare
            if (IsEqual<T>(current, expectedValue))
                return current;

            if (Stopwatch.GetTimestamp() - start >= timeoutTicks)
                throw new TimeoutException(
                    $" Expected value '{expectedValue}' was not reached within {timeout}.For nodeId with Browsename: {GetBrowsename(nodeId)}");

            await Task.Delay(interval).ConfigureAwait(false);
        }
    }

    private static bool IsEqual<T>(T a, T b)
    {

        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;


        if (a is LocalizedText ltA && b is LocalizedText ltB)
        {
            return string.Equals(ltA.Locale, ltB.Locale, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(ltA.Text, ltB.Text, StringComparison.Ordinal);
        }

        return EqualityComparer<T>.Default.Equals(a, b);
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

    private readonly Dictionary<uint, Action<DataValue>> _dataChange = new();
    private uint _subscriptionId;
    private uint _nextHandle = 1;
    private readonly object _subLock = new();

    private void HandleDataChange(uint handle, DataValue dv)
    {
        if (_dataChange.TryGetValue(handle, out var cb))
            cb(dv);
    }

    // -----------------------------------------------------------------------------
    // 3)  NEUE METHODE in OpcUaSession --------------------------------------------
    // -----------------------------------------------------------------------------
    public Task<(T oldValue, T newValue)> RegisterChangedEventOnVar<T>(
            NodeId nodeId,
            int timeoutMs = 1_000)
    {
        // alten Wert auslesen
        T oldVal = GetValue<T>(nodeId);

        // Subscription nur einmal anlegen
        if (_subscriptionId == 0)
        {
            lock (_subLock)
            {
                if (_subscriptionId == 0)
                {
                    StatusCode sc = _client.CreateSubscription(
                                        0,      // publishingInterval (0 = Serverwahl)
                                        1_000,  // lifeTimeCount
                                        true,   // publishingEnabled
                                        0,      // priority
                                        out _subscriptionId);
                    if (sc != StatusCode.Good)
                        throw new Exception("CreateSubscription fehlgeschlagen");

                    _client.SetPublishingMode(true, new[] { _subscriptionId }, out _);
                }
            }
        }

        // MonitoredItem anlegen
        uint handle = _nextHandle++;
        var req = new MonitoredItemCreateRequest(
                      new ReadValueId(nodeId, NodeAttribute.Value, null, new QualifiedName()),
                      MonitoringMode.Reporting,
                      new MonitoringParameters(handle, 0, null, 1, true));

        _client.CreateMonitoredItems(_subscriptionId,
                                     TimestampsToReturn.Both,
                                     new[] { req },
                                     out var results);
        if (results[0].StatusCode != StatusCode.Good)
            throw new Exception("CreateMonitoredItems fehlgeschlagen");

        // TaskCompletionSource vorbereiten
        var tcs = new TaskCompletionSource<(T, T)>();
        _dataChange[handle] = dv =>
        {
            if (_dataChange.Remove(handle))
            {
                T newVal = (T)dv.Value;
                tcs.TrySetResult((oldVal, newVal));

                // Aufräumen – MonitoredItem wieder entfernen
                _client.DeleteMonitoredItems(_subscriptionId,
                                             new[] { results[0].MonitoredItemId },
                                             out _);
            }
        };

        // Timeout absichern
        if (timeoutMs > 0)
        {
            _ = Task.Delay(timeoutMs).ContinueWith(_ =>
            {
                if (_dataChange.Remove(handle))
                {
                    tcs.TrySetException(new TimeoutException(
                        $"Kein Wertwechsel für {nodeId} innerhalb {timeoutMs} ms."));
                }
            });
        }

        return tcs.Task;
    }


}


/// <summary>
/// Leitet von LibUA.Client ab und feuert ein Event,
/// sobald DataChange‑Notifications eintreffen.
/// </summary>
public class NotifyingClient : Client
    {
        public event Action<uint, DataValue> DataChange;

        public NotifyingClient(string host, int port = 4840, int timeoutMs = 10_000)
            : base(host, port, timeoutMs) { }

        public override void NotifyDataChangeNotifications(uint subId,
                                                           uint[] clientHandles,
                                                           DataValue[] notifications)
        {
            for (int i = 0; i < clientHandles.Length; i++)
                DataChange?.Invoke(clientHandles[i], notifications[i]);
        }
    }
