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
using System.Collections.Generic;
using System.Linq;
using CoT;
using System.Threading;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_ActiveAlarmsCounterLogic : BaseNetLogic
{
    public static RT_CoT_ActiveAlarmsCounterLogic ServerInstance;

    private IUAVariable _messageCount_All;
    private IUAVariable _messageCount_CriticalFault;
    private IUAVariable _messageCount_Fault;
    private IUAVariable _messageCount_Warning;
    private IUAVariable _messageCount_Action;
    private IUAVariable _messageCount_Info;
    private LongRunningTask _initialization;

    public override void Start()
    {
        try
        {
            _messageCount_All = GetVariable("messageCount_All", LogicObject);
            _messageCount_CriticalFault = GetVariable("messageCount_CriticalFault", LogicObject);
            _messageCount_Fault = GetVariable("messageCount_Fault", LogicObject);
            _messageCount_Warning = GetVariable("messageCount_Warning", LogicObject);
            _messageCount_Action = GetVariable("messageCount_Action", LogicObject);
            _messageCount_Info = GetVariable("messageCount_Info", LogicObject);

            if (ServerInstance == null)
            {
                ServerInstance = this;
            }
            else
            {
                Logger.CreateAlarmAndLog("Server script instantiated twice");
            }
        }
        catch (Exception ex)
        {
            Logger.CreateAlarmAndLog("AlarmMessageCounter Init Fault", MessageType.Warning, ex, LogicObject);
        }

        _initialization = new LongRunningTask(Initialize, LogicObject);
        _initialization.Start();
    }

    private void Initialize()
    {
        while (RT_CoT_ActiveAlarmsObserverLogic.ServerInstance == null)
        {
            if (_initialization.IsCancellationRequested)
            {
                return;
            }
            Thread.Sleep(10);
        }
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmOccurred += OnAlarmListChanged;
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmGone += OnAlarmListChanged;
    }

    public override void Stop()
    {
        _initialization?.Dispose();
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmOccurred -= OnAlarmListChanged;
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmGone -= OnAlarmListChanged;
    }

    private void UpdateCounters()
    {
        _messageCount_All.Value = RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms.Count;
        _messageCount_CriticalFault.Value = RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms
        .Select(alarmNode => alarmNode as UAObject)
        .Where(alarmNode => alarmNode != null && (MessageType)alarmNode.GetVariable("messageType").Value.Value == MessageType.CriticalFault)
        .Count();

        _messageCount_Fault.Value = RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms
        .Select(alarmNode => alarmNode as UAObject)
        .Where(alarmNode => alarmNode != null && (MessageType)alarmNode.GetVariable("messageType").Value.Value == MessageType.Fault)
        .Count();

        _messageCount_Warning.Value = RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms
        .Select(alarmNode => alarmNode as UAObject)
        .Where(alarmNode => alarmNode != null && (MessageType)alarmNode.GetVariable("messageType").Value.Value == MessageType.Warning)
        .Count();

        _messageCount_Action.Value = RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms
        .Select(alarmNode => alarmNode as UAObject)
        .Where(alarmNode => alarmNode != null && (MessageType)alarmNode.GetVariable("messageType").Value.Value == MessageType.Action)
        .Count();

        _messageCount_Info.Value = RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms
        .Select(alarmNode => alarmNode as UAObject)
        .Where(alarmNode => alarmNode != null && (MessageType)alarmNode.GetVariable("messageType").Value.Value == MessageType.Info)
        .Count();

    }

    private void OnAlarmListChanged(object sender, EventArgs e)
    {
        UpdateCounters();
    }
}
