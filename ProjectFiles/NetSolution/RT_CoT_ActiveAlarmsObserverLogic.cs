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
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
using static RT_CoT_NetLogicAlarmsLogic;
#endregion

public class RT_CoT_ActiveAlarmsObserverLogic : BaseNetLogic
{
    // See: https://github.com/FactoryTalk-Optix/NetLogic_CheatSheet/blob/25376ef7fbd3669b6d27dd6f59cb6404ae906fe0/pages/alarming.md

    public static RT_CoT_ActiveAlarmsObserverLogic ServerInstance;

    public List<IUANode> ActiveAlarms;
    public event EventHandler<ActiveAlarmsChangedEventArgs> AlarmOccurred;
    public event EventHandler<ActiveAlarmsChangedEventArgs> AlarmGone;

    private IEventRegistration _alarmsCreationObserver;
    private uint _affinityId;

    public override void Start()
    {
        Log.Info("Starting ActiveAlarmsObserverLogic");

        // Get the affinity ID of the current NetLogic to avoid cross thread issues
        _affinityId = LogicObject.Context.AssignAffinityId();

        // Create the observer to check when alarms are created and/or deleted
        StartObserver();

        if (ServerInstance == null)
        {
            ServerInstance = this;
        }
        else
        {
            //TODO: throw error "Server script instantiated twice"
        }
    }

    public override void Stop()
    {
        Log.Info("Stopping ActiveAlarmsObserverLogic");
        _alarmsCreationObserver?.Dispose();
    }

    public void StartObserver()
    {
        // Get the alarms server object
        var retainedAlarmsObject = LogicObject.Context.GetNode(FTOptix.Alarm.Objects.RetainedAlarms);

        // Get the object containing the actual list of alarms
        var localizedAlarmsObject = retainedAlarmsObject.GetVariable("LocalizedAlarms");
        if (localizedAlarmsObject == null)
        {
            Logger.CreateAlarmAndLog("AlarmsObserverLogic: LocalizedAlarms variable not found in retainedAlarmsObject");

            return;
        }

        var localizedAlarmsNodeId = (NodeId)localizedAlarmsObject.Value;
        IUANode localizedAlarmsContainer = null;
        if (localizedAlarmsNodeId?.IsEmpty == false)
            localizedAlarmsContainer = LogicObject.Context.GetNode(localizedAlarmsNodeId);
        if (localizedAlarmsContainer == null)
        {
            Logger.CreateAlarmAndLog("AlarmsObserverLogic: LocalizedAlarms node not found");
            return;
        }

        ActiveAlarms = new();
        foreach (var localizedAlarm in localizedAlarmsContainer.Children)
        {
            ActiveAlarms.Add(localizedAlarm);
        }

        // Create a new custom alarms observer
        var alarmsObserver = new AlarmsObserver();
        // Register the observer to the server node
        _alarmsCreationObserver = localizedAlarmsContainer.RegisterEventObserver(
            alarmsObserver, // Which observer to use
            EventType.ForwardReferenceAdded | // Register when alarms are created
            EventType.ForwardReferenceRemoved, // Register when alarms are disposed
            _affinityId); // Pass the affinity ID
    }

    public bool AddAlarmToList(IUANode alarm)
    {
        if (ActiveAlarms.Contains(alarm))
        {
            return false;
        }
        else
        {
            ActiveAlarms.Add(alarm);
            ActiveAlarmsChangedEventArgs args = new();
            args.newValue = alarm;
            AlarmOccurred?.Invoke(this, args);
            return true;
        }
    }

    public bool RemoveAlarmFromList(IUANode alarm)
    {
        if (ActiveAlarms.Contains(alarm))
        {
            ActiveAlarms.Remove(alarm);
            ActiveAlarmsChangedEventArgs args = new();
            args.oldValue = alarm;
            AlarmGone?.Invoke(this, args);
            return true;
        }
        else
        {
            return false;
        }
    }

    public class ActiveAlarmsChangedEventArgs : EventArgs
    {
        public IUANode newValue { get; set; }
        public IUANode oldValue { get; set; }
    }

    class AlarmsObserver : IReferenceObserver
    {
        public void OnReferenceAdded(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            if (RT_CoT_ActiveAlarmsObserverLogic.ServerInstance != null)
            {
                RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AddAlarmToList(targetNode);
            }
        }

        public void OnReferenceRemoved(IUANode sourceNode, IUANode targetNode, NodeId referenceTypeId, ulong senderId)
        {
            if (RT_CoT_ActiveAlarmsObserverLogic.ServerInstance != null)
            {
                RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.RemoveAlarmFromList(targetNode);
            }
        }
    }
}
