#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.SQLiteStore;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_CMP_Dialog : BaseNetLogic
{
    IUAVariable overlayVariable;
    public override void Start()
    {
        try
        {
            overlayVariable = GetVariable("overlay", LogicObject);
            RT_CoT_Helper.SetVariableValue<bool>(overlayVariable, true);
        }
        catch (Exception ex)
        {
            Logger.CreateAlarmAndLog("OverlayControl Init Fault", MessageType.Warning, ex, LogicObject);
        }
    }

    public override void Stop()
    {
        try
        {
            //overlayVariable.RemoteWrite(false);
            overlayVariable.Value = false;
        }
        catch (Exception ex)
        {
            Log.Error("OverlayControl", $"Error in Stop method: {ex.Message}");
        }
    }


}
