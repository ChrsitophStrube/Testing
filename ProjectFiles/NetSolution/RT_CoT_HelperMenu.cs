#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.Report;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
#endregion

public class RT_CoT_HelperMenu : BaseNetLogic
{
    private IUAVariable _helperMenuActive;
    public override void Start()
    {
        _helperMenuActive = GetVariable("helperMenuActive", LogicObject);
    }

    public override void Stop()
    {
        _helperMenuActive.Value = false;
    }
}
