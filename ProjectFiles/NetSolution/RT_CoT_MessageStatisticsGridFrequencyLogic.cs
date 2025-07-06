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
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_MessageStatisticsGridFrequencyLogic : BaseNetLogic
{
    IUAVariable _toVariable;
    IUAVariable _fromVariable;

    private IUAVariable _criticalFilter;
    private IUAVariable _errorFilter;
    private IUAVariable _warninglFilter;
    private IUAVariable _actionFilter;
    private IUAVariable _infoFilter;

    private IUAVariable _alarmHistoryStore;
    private string _alarmHistoryTable;
    private int _resultRecordQuantity = 10;

    private class StatisticsIntermediateResult
    {
        public StatisticsIntermediateResult(int id, string group, int messageType, string message)
        {
            this.ID = id;
            this.Group =  group;
            this.MessageType = messageType;
            this.Message = message;
            this.Frequency = 0;         
        }

        public int ID { get; private set;}        
        public string Group { get; set;}
        public string SourceVariable { get; set;}
        public string Message { get; set;}
        public int Frequency { get; set;}
        public int MessageType { get; set;}
    }

    public override void Start()
    {
       // After checking validity, we set a default time interval of 24 hours
        _toVariable = LogicObject.GetVariable("To");
        if (_toVariable == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing To variable");
            return;
        }

        _toVariable.Value = DateTime.Now;
        _fromVariable = LogicObject.GetVariable("From");
        if (_fromVariable == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing From variable");
            return;
        }

        _fromVariable.Value = DateTime.Now.AddHours(-24);

        _alarmHistoryStore = LogicObject.GetVariable("alarmHistoryStore");
        if (_fromVariable == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing alarmHistoryStore variable");
            return;
        }

        var alarmHistoryTableNode = LogicObject.GetVariable("alarmsEventLoggerName");
        if (alarmHistoryTableNode == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing alarmsEventLoggerName variable");
            return;
        }        
        _alarmHistoryTable = alarmHistoryTableNode.Value;
        if (_alarmHistoryTable == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing alarmsEventLoggerName variable value");
            return;
        }  

        _criticalFilter = LogicObject.GetVariable("CriticalSelected");
        if (_criticalFilter == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing CriticalSelected variable");
            return;
        }

        _errorFilter = LogicObject.GetVariable("ErrorSelected");
        if (_errorFilter == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing ErrorSelected variable");
            return;
        }

        _warninglFilter = LogicObject.GetVariable("WarningSelected");
        if (_warninglFilter == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing WarningSelected variable");
            return;
        }

        _actionFilter = LogicObject.GetVariable("ActionSelected");
        if (_actionFilter == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing ActionSelected variable");
            return;
        }

        _infoFilter = LogicObject.GetVariable("InfoSelected");
        if (_infoFilter == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing InfoSelected variable");
            return;
        }

        var resultRecords = LogicObject.GetVariable("ResultRecords");
        if (resultRecords == null)
        {
            Log.Error("RT_CoT_MessageStatisticsGridFrequencyLogic", "Missing ResultRecords variable");
            return;            
        }
        
        if (resultRecords.Value > 0)
        {
            _resultRecordQuantity = resultRecords.Value;
        }

        this.GetStatistics();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void Refresh()
    {
        if (_toVariable != null)
        {
            _toVariable.Value = DateTime.Now;
        }

        this.GetStatistics();        
    }

    [ExportMethod]
    public void GetStatistics()
    {
        try
        {
            var lang = Session.ActualLanguage;
            DateTime fromDateTime = _fromVariable.Value;
            string fromDateTimeText = fromDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
            DateTime toDateTime = _toVariable.Value;
            string toDateTimeText = toDateTime.ToString("yyyy-MM-ddTHH:mm:ss");
            int infoSelected = (bool)_infoFilter.Value ? 1 : 0;
            int actionSelected = (bool)_actionFilter.Value ? 1 : 0;
            int warningSelected = (bool)_warninglFilter.Value ? 1 : 0;
            int errorSelected = (bool)_errorFilter.Value ? 1 : 0;
            int criticalSelected = (bool)_criticalFilter.Value ? 1 : 0;

            string sqlQuery = $"SELECT messageType, \"group\", \"messageTextStatic_{lang}\" AS Message, messageTextDynamic, " 
                    + $"LocalTime, occurredTime, id FROM {_alarmHistoryTable} "
                    + $"WHERE LocalTime BETWEEN '{fromDateTimeText}' AND '{toDateTimeText}' AND ActiveState_Id = 0 AND occurredTime > '2000-01-01 00:00:00' "
                    + $"AND ((messageType = 1 AND {infoSelected} = 1) OR (messageType = 2 AND {warningSelected} = 1) "
                    +   $"OR (messageType = 3 AND {actionSelected} = 1) OR (messageType = 4 AND {errorSelected} = 1) "
                    +   $"OR (messageType = 5 AND {criticalSelected} = 1)) ";

            Log.Info(sqlQuery);

            Object[,] data;
            String[] headers;
            Store dataStore = (Store)InformationModel.Get(_alarmHistoryStore.Value);            
            dataStore.Query(sqlQuery, out headers, out data);

            // Find the index of columns
            int disappearedTimeIndex = Array.IndexOf(headers, "LocalTime");
            int occurredTimeIndex = Array.IndexOf(headers, "occurredTime");
            int groupIndex = Array.IndexOf(headers, "group");            
            int messageIndex = Array.IndexOf(headers, "Message");
            int messageDynIndex = Array.IndexOf(headers, "messageTextDynamic");
            int idIndex = Array.IndexOf(headers, "id"); 
            int messageTypeIndex = Array.IndexOf(headers, "messageType"); 
            
            // Create a dictionary to store start and end times for each alarm ID
            Dictionary<int, StatisticsIntermediateResult> intermediateResults = new Dictionary<int, StatisticsIntermediateResult>();
            double frequencyTotalAllEntries = 0.0;

            // Iterate over the rows of data
            for (int i = 0; i < data.GetLength(0); i++)
            {
                int alarmId = Convert.ToInt32(data[i, idIndex]);
                StatisticsIntermediateResult intermediateResult;

                // Initialize the list for the alarm name if it doesn't exist
                if (!intermediateResults.ContainsKey(alarmId))
                {
                    string group = data[i, groupIndex].ToString();
                    int messageType = Convert.ToInt32(data[i, messageTypeIndex]);
                    string message = data[i, messageIndex].ToString();
                    string messageDyn = data[i, messageDynIndex].ToString();

                    intermediateResult = new StatisticsIntermediateResult(alarmId, group, messageType, message + ' ' + messageDyn);
                    intermediateResults[alarmId] = intermediateResult;
                }
                else
                {
                    intermediateResult = intermediateResults[alarmId];
                }

                intermediateResult.Frequency++;
                frequencyTotalAllEntries++;
            }

            var target = InformationModel.Get(LogicObject.GetVariable("statisticsResult").Value);
            target.Children.Clear();
            double frequencyBarMaxPercentage = 0.0;

            foreach (var intermediateResult in intermediateResults.Values.OrderByDescending(x => x.Frequency).Take(_resultRecordQuantity).ToList())
            {
                CoT_MessageStatisticsFrequencyResult res = InformationModel.Make<CoT_MessageStatisticsFrequencyResult>("Message_" + intermediateResult.ID);
                double frequencyPercentage = intermediateResult.Frequency / frequencyTotalAllEntries * 100.0;
                if (frequencyBarMaxPercentage == 0.0)
                {
                    frequencyBarMaxPercentage = frequencyPercentage * 1.2;
                }
                string frequencyBar = string.Empty;
                frequencyBar = frequencyBar.PadRight((int)(frequencyPercentage / frequencyBarMaxPercentage * 50.0 + 0.5), 'l');

                res.GetVariable("ID").Value = intermediateResult.ID;
                res.GetVariable("MessageType").Value = intermediateResult.MessageType;
                res.GetVariable("Group").Value = intermediateResult.Group;
                res.GetVariable("Message").Value = intermediateResult.Message;
                res.GetVariable("Frequency").Value = intermediateResult.Frequency;
                res.GetVariable("FrequencyPercentage").Value = frequencyPercentage;
                res.GetVariable("FrequencyBar").Value = frequencyBar;

                target.Add(res);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
}
