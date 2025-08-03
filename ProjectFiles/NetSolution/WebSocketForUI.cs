#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.WebUI;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json.Nodes;
#endregion

public class WebSocketForUI : BaseNetLogic
{
    private ClientWebSocket _webSocket;
    private CancellationTokenSource _cts;
    private Task _receiveTask;
    private Timer _reconnectTimer;

    public override void Start()
    {
        TryConnect();
        _reconnectTimer = new Timer(ReconnectIfNeeded, null, 5000, 5000); // alle 5 Sekunden prüfen
        Thread.Sleep(8000);
        UAValue popupNodeId = LogicObject.Get<NodePointer>("updatePopuop").Value;
        var popup = InformationModel.Get<DialogType>(popupNodeId);
        //OpenDialogBox(popup);

    }

    public override void Stop()
    {
        _cts?.Cancel();
        _webSocket?.Dispose();
        _reconnectTimer?.Dispose();
    }

    private async void TryConnect()
    {
        try
        {
            string ip = LogicObject.GetVariable("WS_IP")?.Value?.ToString() ?? "localhost";
            Log.Warning(ip);
            //int port = Convert.ToInt32((Int32)LogicObject.GetVariable("WS_Port")?.Value);
            string port = LogicObject.GetVariable("WS_Port")?.Value?.ToString() ?? "5000";
            string channel = LogicObject.GetVariable("WS_Channel")?.Value?.ToString() ?? "default";

            string uri = $"ws://nodeserver-a7cfb069.azurewebsites.net/ws?channel=master";
            Log.Warning(uri);
            _cts = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();

            await _webSocket.ConnectAsync(new Uri(uri), _cts.Token);
            Log.Info($"Connected to WebSocket: {uri}");

            _receiveTask = ReceiveLoop(_cts.Token);
        }
        catch (Exception ex)
        {
            Log.Warning("WebSocket connection failed: " + ex.Message);
        }
    }

    private async Task ReceiveLoop(CancellationToken token)
    {
        var buffer = new byte[1024];

        while (_webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Log.Warning("WebSocket closed by server.");
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", token);
                break;
            }

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            HandleMessage(message);
        }
    }

    private void HandleMessage(string message)
    {
        try
        {
            Log.Info("WS MSG: " + message);

            if (message == "openPopup")
            {
                UAValue popupNodeId = LogicObject.Get<NodePointer>("updatePopuop").Value;
                var popup = InformationModel.Get<DialogType>(popupNodeId);
                OpenDialogBox(popup);


            }
            else if (message == "showMessage")
            {
                ShowAlert("Default alert");
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to handle WS message: " + ex.Message);
        }
    }

    private void ShowPopup(string popupId, NodeId sessionID)
    {
        var popup = Project.Current.FindObject(popupId) as DialogType
    ;
        if (popup != null)
        {
            UISession _uiSession = (UISession)InformationModel.Get(sessionID);
            var _mainWindows = _uiSession.Get("UIRoot");

            UICommands.OpenDialog(_uiSession, popup);
            Log.Info("Opened popup: " + popupId);
        }
        else
        {
            Log.Warning("Popup not found: " + popupId);
        }
    }

    private void ShowAlert(string message)
    {
        Log.Info("ALERT: " + message);
        // Optional: Set variable value → UI bound to it
    }

    private void ReconnectIfNeeded(object state)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            Log.Warning("Reconnecting WebSocket...");
            TryConnect();
        }
    }

    public void OpenDialogBox(DialogType dialog, NodeId[] AliasArray = null)
    {
        var wpe = Project.Current.Get("UI/WebPresentationEngine");
        var wpeSessions = wpe.Get("Sessions");
        foreach (var wpeSession in wpeSessions.Children)
        {
            var wpeWindow = wpeSession.Get("UIRoot");
            if (wpeWindow != null)
            {
                UICommands.OpenDialog(wpeWindow, dialog);
            }
        }
    }


    [ExportMethod]
    public void StartUpdate()
    {
        Log.Info("Update", "StartUpdate");
    }

    [ExportMethod]
    public void DelayUpdate()
    {
        Log.Info("Update", "DelayUpdate");
    }
}
