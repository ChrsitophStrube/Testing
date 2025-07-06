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
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using CoT;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_MessageLineLogic : BaseNetLogic
{
    private LongRunningTask _attachToActiveAlarmsObserver;
    private LongRunningTask _attachToPlcAlarmsMapping;

    private IUAVariable _mostRelevantMessage;

    public override void Start()
    {
        try
        {
            _attachToActiveAlarmsObserver = new LongRunningTask(AttachToActiveAlarmsObserver, LogicObject);
            _attachToActiveAlarmsObserver.Start();
            _attachToPlcAlarmsMapping = new LongRunningTask(AttachToPlcAlarmsMapping, LogicObject);
            _attachToPlcAlarmsMapping.Start();

            _mostRelevantMessage = GetVariable("mostRelevantMessage", LogicObject);
        
        }
        catch (Exception ex)
        {
            Logger.CreateAlarmAndLog("Messageline Init Fault", MessageType.Warning, ex, LogicObject);
        }
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmOccurred -= OnAlarmListChanged;
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmGone -= OnAlarmListChanged;
        RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm.VariableChange -= OnStopReasonAlarmChanged;
    }

    private void AttachToActiveAlarmsObserver()
    {
        while (RT_CoT_ActiveAlarmsObserverLogic.ServerInstance == null)
        {
            if (_attachToActiveAlarmsObserver.IsCancellationRequested)
            {
                return;
            }
            Thread.Sleep(10);
        }
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmOccurred += OnAlarmListChanged;
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmGone += OnAlarmListChanged;
    }

    private void AttachToPlcAlarmsMapping()
    {
        while (RT_CoT_PlcAlarmsMappingLogic.ServerInstance == null)
        {
            if (_attachToPlcAlarmsMapping.IsCancellationRequested)
            {
                return;
            }
            Thread.Sleep(10);
        }
        while (RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm == null)
        {
            if (_attachToPlcAlarmsMapping.IsCancellationRequested)
            {
                return;
            }
            Thread.Sleep(10);
        }
        RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm.VariableChange += OnStopReasonAlarmChanged;
    }

    private void OnStopReasonAlarmChanged(object sender, VariableChangeEventArgs e)
    {
        SelectAlarmForMessageLine();
    }

    private void OnAlarmListChanged(object sender, EventArgs e)
    {
        SelectAlarmForMessageLine();
    }

    private void SelectAlarmForMessageLine()
    {
        if (RT_CoT_ActiveAlarmsObserverLogic.ServerInstance != null && RT_CoT_PlcAlarmsMappingLogic.ServerInstance != null)
        {
            if ((NodeId)RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm.Value != null)
            {
                IUANode newStopReason = InformationModel.Get(RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm.Value);
                if (newStopReason != null)
                {
                    _mostRelevantMessage.Value = newStopReason.NodeId;
                }
            }
            else
            {
                NodeId alarmSelected = null;
                List<IUANode> activeAlarmsSnapshot = new(RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.ActiveAlarms);

                if (activeAlarmsSnapshot.Count > 0)
                {
                    alarmSelected = activeAlarmsSnapshot
                        .Select(alarmNode => alarmNode as UAObject)                                              // Retrieve alarm node
                        .Where(alarmNode => alarmNode != null)                                                   // Ensure the alarm node is valid
                        .Select(alarmNode => new
                        {
                            NodeId = alarmNode.NodeId,
                            Severity = (ushort?)alarmNode.GetVariable("Severity")?.Value?.Value ?? 0,
                            MessageType = (int?)(alarmNode.GetVariable("messageType")?.Value?.Value) ?? (int)MessageType.None, // Default to MessageType.None if missing
                            Time = (alarmNode.GetVariable("Time")?.Value is UAValue timeValue && timeValue.Value is DateTime dt)
                                ? dt
                                : DateTime.MinValue                                                             // Default to earliest time if missing
                        })
                        .OrderByDescending(alarm => alarm.Severity)                                             // Highest severity first
                        .ThenByDescending(alarm => alarm.MessageType)                                           // Highest message type first
                        .ThenBy(alarm =>
                            (alarm.MessageType == (int)MessageType.CriticalFault ||
                            alarm.MessageType == (int)MessageType.Fault ||
                            alarm.MessageType == (int)MessageType.Action)
                                ? alarm.Time.Ticks                                                              // Oldest first (smallest ticks first)
                                : -alarm.Time.Ticks)                                                            // Newest first (negate ticks for descending order)
                        .Select(alarm => alarm.NodeId)                                                          // Select only the NodeId
                        .FirstOrDefault();
                }

                // Assign the alarmSelected to the Optix variable.
                if (alarmSelected != null)
                {
                    _mostRelevantMessage.Value = alarmSelected;
                }
                else
                {
                    _mostRelevantMessage.Value = NodeId.Empty;
                }
            }
        }
    }
}
