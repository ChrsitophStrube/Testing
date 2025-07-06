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
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_AlarmHistoryStoreLogic : BaseNetLogic
{
    private LongRunningTask _attachToActiveAlarmsObserver;
    private LongRunningTask _attachToPlcAlarmsMapping;

    private IUAVariable _alarmHistoryStore;
    private IUAVariable _alarmHistoryTableName;

    private Timer _referenceChangedDelayTimer;
    private bool _anyReferenceRemoved = false;
    private const uint _referenceChangedDelay = 100;

    public override void Start()
    {
        try
        {
            _alarmHistoryStore = GetVariable("alarmHistoryStore", LogicObject);
            _alarmHistoryTableName = GetVariable("alarmHistoryTableName", LogicObject);
        }
        catch (Exception ex)
        {
            Logger.CreateAlarmAndLog("AlarmHistory Init Fault", MessageType.Warning, ex, LogicObject);
        }

        _attachToActiveAlarmsObserver = new LongRunningTask(AttachToActiveAlarmsObserver, LogicObject);
        _attachToActiveAlarmsObserver.Start();
        _attachToPlcAlarmsMapping = new LongRunningTask(AttachToPlcAlarmsMapping, LogicObject);
        _attachToPlcAlarmsMapping.Start();

        _referenceChangedDelayTimer = new Timer(this.OnReferenceChangedDelayTimerElapsed,
            null, Timeout.Infinite, Timeout.Infinite);
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

    private void OnAlarmListChanged(object sender, RT_CoT_ActiveAlarmsObserverLogic.ActiveAlarmsChangedEventArgs e)
    {
        if (e.newValue == null)
        {
            // no new value so, so an alarm went away
            _anyReferenceRemoved = true;
        }

        if (_referenceChangedDelayTimer != null)
        {
            _referenceChangedDelayTimer.Change(_referenceChangedDelay, Timeout.Infinite);
        }
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
        RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm.VariableChange += OnStopReasonAlarmChanged;
    }

    private void OnStopReasonAlarmChanged(object sender, VariableChangeEventArgs e)
    {
        IUANode stopReasonAlarm = InformationModel.Get(e.NewValue);
        if (stopReasonAlarm != null)
        {
            int stopReasonAlarmId = stopReasonAlarm.GetVariable("id")?.Value;
            if (stopReasonAlarmId != 0)
            {
                string sqlQuery = $"select max(LocalTime), ActiveState_Id, stopReason from {_alarmHistoryTableName.Value.Value} where id = {stopReasonAlarmId}";

                Object[,] data;
                String[] headers;
                Store dataStore = (Store)InformationModel.Get(_alarmHistoryStore.Value);
                dataStore.Query(sqlQuery, out headers, out data);

                if ((data.GetLength(0) > 0) && (data.GetLength(1) >= 3) && (data[0, 0] != null))
                {
                    string maxLocalTime = data[0, 0].ToString();
                    int activeStateId = Convert.ToInt32(data[0, 1]);
                    int stopReason = Convert.ToInt32(data[0, 2]);

                    if ((activeStateId == 1) && (stopReason != 1))
                    {
                        string sqlUpdateCmd = $"update {_alarmHistoryTableName.Value.Value} set stopReason = 1 where id = {stopReasonAlarmId} and ActiveState_Id = 1 and LocalTime = '{maxLocalTime}'";
                        dataStore.Query(sqlUpdateCmd, out headers, out data);
                    }
                }
            }
        }
    }

    public override void Stop()
    {
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmOccurred -= OnAlarmListChanged;
        RT_CoT_ActiveAlarmsObserverLogic.ServerInstance.AlarmGone -= OnAlarmListChanged;
        RT_CoT_PlcAlarmsMappingLogic.ServerInstance.StopReasonAlarm.VariableChange -= OnStopReasonAlarmChanged;
    }

    private void OnReferenceChangedDelayTimerElapsed(object state)
    {
        Log.Verbose1("OnReferenceChangedDelayTimerElapsed");

        bool updateAlarmHistory = false;

        if (_anyReferenceRemoved)
        {
            updateAlarmHistory = true;
            _anyReferenceRemoved = false;
        }

        if (updateAlarmHistory)
        {
            SetOccurredTimeInAlarmHistory();
        }
    }

    private void SetOccurredTimeInAlarmHistory()
    {
        Log.Verbose1("SetOccurredTimeInAlarmHistory");

        string sqlQuery = $"select LocalTime, id, ActiveState_Id from {_alarmHistoryTableName.Value.Value} where occurredTime is NULL or occurredTime < '20000101 00:00:00' order by LocalTime";

        Object[,] data;
        String[] headers;

        Store dataStore = (Store)InformationModel.Get(_alarmHistoryStore.Value);
        dataStore.Query(sqlQuery, out headers, out data);

        Dictionary<int, AlarmTimeSet> alarmTimes = new Dictionary<int, AlarmTimeSet>();

        if (data.GetLength(1) < 3)
        {
            // TODO : system fehler ausgeben, dies eigentlich nicht auftreten kann
            return;
        }

        Object[,] updateData;
        String[] updateHeaders;

        // Iterate over the rows of data
        for (int i = 0; i < data.GetLength(0); i++)
        {
            DateTime localTime = Convert.ToDateTime(data[i, 0]);
            int alarmId = Convert.ToInt32(data[i, 1]);
            int activeStateId = Convert.ToInt32(data[i, 2]);
            AlarmTimeSet alarmTime;

            // check for existing entry for alarm id in the list or create a new one
            if (alarmTimes.ContainsKey(alarmId))
            {
                alarmTime = alarmTimes[alarmId];
            }
            else
            {
                alarmTime = new AlarmTimeSet();
                alarmTimes[alarmId] = alarmTime;
            }

            // check required action
            if (activeStateId == 1)
            {
                // check for new active event while the last one was still active
                if (alarmTime.ActiveTime.HasValue)
                {
                    // close last entry without related inactive event by setting occurred time for it
                    string activeTimeStr = alarmTime.ActiveTime.Value.ToString("o");
                    string sqlUpdateCmd = $"update {_alarmHistoryTableName.Value.Value} set occurredTime = LocalTime, duration = '0.00:00:00' where id = {alarmId} and ActiveState_Id = 1 and LocalTime = '{activeTimeStr}'";
                    dataStore.Query(sqlUpdateCmd, out updateHeaders, out updateData);
                }

                alarmTime.ActiveTime = localTime;
            }
            else // activeStateId == 0
            {
                if (alarmTime.ActiveTime.HasValue)
                {
                    string activeTimeStr = alarmTime.ActiveTime.Value.ToString("o");
                    string sqlUpdateCmd = $"update {_alarmHistoryTableName.Value.Value} set occurredTime = '{activeTimeStr}' where id = {alarmId} and ActiveState_Id = 1 and LocalTime = '{activeTimeStr}'";
                    dataStore.Query(sqlUpdateCmd, out updateHeaders, out updateData);

                    TimeSpan duration = localTime - alarmTime.ActiveTime.Value;
                    string durationStr = duration.ToString("d\\.hh\\:mm\\:ss");
                    sqlUpdateCmd = $"update {_alarmHistoryTableName.Value.Value} set occurredTime = '{activeTimeStr}', duration = '{durationStr}' where id = {alarmId} and ActiveState_Id = 0 and LocalTime = '{localTime.ToString("o")}'";
                    dataStore.Query(sqlUpdateCmd, out updateHeaders, out updateData);
                }
                else // proceed inactive event without related active event by setting occurred time for it
                {
                    // close last entry without related active event by setting occurred time for it
                    string sqlUpdateCmd = $"update {_alarmHistoryTableName.Value.Value} set occurredTime = LocalTime, duration = '0.00:00:00' where id = {alarmId} and ActiveState_Id = 0 and LocalTime = '{localTime.ToString("o")}'";
                    dataStore.Query(sqlUpdateCmd, out updateHeaders, out updateData);
                }

                // remove fully processed alarm entry from alarmTime list
                alarmTimes.Remove(alarmId);
            }
        }
    }

    private class AlarmTimeSet
    {
        public DateTime? ActiveTime { get; set; }
        public DateTime? InactiveTime { get; set; }
    }
}
