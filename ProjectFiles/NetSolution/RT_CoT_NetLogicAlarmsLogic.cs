#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.RAEtherNetIP;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.NetLogic;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using static RT_CoT_PlcAlarmsMappingLogic;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;
using System.Linq;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using CoT;
using System.Diagnostics;
using FTOptix.Recipe;
using FTOptix.Report;
using FTOptix.DataLogger;
using System.Buffers;
using System.Text;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_NetLogicAlarmsLogic : BaseNetLogic
{
    static Folder _systemAlarmsFolder;
    static bool _SupressMode;
    static Dictionary<MessageType, AlarmVisibilityObj> _alarmVisibilities;
    int _autoIncrementID = -1;
    static string _actualLocaleId;
    static RT_CoT_NetLogicAlarmsLogic rt_CoT_NetLogicAlarmsLogic = null;
    static readonly object _padlock = new();
    private readonly object _systemAlarmLock = new();
    private readonly object _LogLock = new();



    public override void Start()
    {
        Initialize();
    }

    public override void Stop()
    {
        DeleteSystemAlarms();
    }

    private void Initialize()
    {
        try
        {
            _systemAlarmsFolder = GetAliasObject<Folder>("systemAlarmsFolder", LogicObject);
            _actualLocaleId = Session.ActualLocaleId;
            _alarmVisibilities = createAlarmVisibilities(LogicObject.Children);
            _SupressMode = GetVariable("supressMode", LogicObject).GetVariableValue<bool>();
        }
        catch (Exception ex)
        {
            Log.Error("System Alarms Initialization Fault");
        }
    }

    public static RT_CoT_NetLogicAlarmsLogic Logger
    {
        get
        {
            lock (_padlock)
            {
                if (rt_CoT_NetLogicAlarmsLogic == null)
                {
                    rt_CoT_NetLogicAlarmsLogic = new();
                    if (RT_CoT_NetLogicAlarmsLogic._systemAlarmsFolder == null || String.IsNullOrEmpty(_actualLocaleId))
                    {
                        Log.Error("System Alarms logging not initialized");
                    }
                }
                return rt_CoT_NetLogicAlarmsLogic;
            }
        }
    }



    /// <summary>
    ///Creates a System alarm that is visible in the Alarm List during Runtime
    /// </summary>
    /// <param name="message">Message text String Unlocalized</param>
    /// <param name="messageType">Optional: MessageType </param>
    public int CreateAlarmAndLog(string message, MessageType messageType = MessageType.Warning)
    {
        int alarmId = 0;
        LocalizedText textLocalized = new(message, string.Empty);
        if (ShouldSystemAlarmTriggert(messageType))
        {
            alarmId = CreateSystemAlarm(textLocalized, messageType, null, null);
        }
        CreateLogEntry(textLocalized, messageType, null, null, alarmId);
        return alarmId;
    }

    /// <summary>
    ///Creates a System alarm that is visible in the Alarm List during Runtime with additional Exception information
    /// </summary>
    /// <param name="message">Message text String not Localized</param>
    /// <param name="e">Cached Exception </param>
    public int CreateAlarmAndLog(string message, MessageType messageType, Exception e, IUAObject logicObject)
    {
        int alarmId = 0;
        LocalizedText textLocalized = new(message, string.Empty);
        if (ShouldSystemAlarmTriggert(messageType))
        {
            alarmId = CreateSystemAlarm(textLocalized, messageType, e, logicObject);
        }
        CreateLogEntry(textLocalized, messageType, e, logicObject, alarmId);
        return alarmId;
    }

    /// <summary>
    /// Creates a localized System alarm that is visible in the Alarm List during Runtime
    /// </summary>
    /// <param name="localizedMessageKey">Text Key for localized Alarms</param>
    /// <param name="messageType">Optional: MessageType</param>
    public int CreateAlarmAndLogLocalized(string localizedMessageKey, MessageType messageType = MessageType.Warning)
    {
        int alarmId = 0;
        LocalizedText localizedText = new(Project.Current.NodeId.NamespaceIndex, localizedMessageKey);
        if (ShouldSystemAlarmTriggert(messageType))
        {
            alarmId = CreateSystemAlarm(localizedText, messageType, null, null);
        }
        CreateLogEntry(localizedText, messageType, null, null, alarmId);
        return alarmId;
    }

    public int CreateAlarmAndLogLocalized(string localizedMessageKey, MessageType messageType, Exception e, IUAObject logicObject)
    {
        int alarmId = 0;
        LocalizedText localizedText = new(Project.Current.NodeId.NamespaceIndex, localizedMessageKey);
        if (ShouldSystemAlarmTriggert(messageType))
        {
            alarmId = CreateSystemAlarm(localizedText, messageType, e, logicObject);
        }
        CreateLogEntry(localizedText, messageType, e, logicObject, alarmId);
        return alarmId;
    }

    /// <summary>
    /// Resets all Active System Alarms and clean them from the Alarm list
    /// </summary>
    [ExportMethod]
    public static void ResetSystemAlarms()
    {
        IEnumerable<CoT_NetLogicAlarm> systemAlarms = _systemAlarmsFolder.GetNodesByType<CoT_NetLogicAlarm>();

        foreach (CoT_NetLogicAlarm alarm in systemAlarms)
        {
            alarm.InputValue = (int)AlarmState.Reset;
        }
    }

    private int CreateSystemAlarm(LocalizedText messageLocalized, MessageType messageType, Exception e, IUAObject logicObject)
    {
        lock (_systemAlarmLock)
        {
            CoT_NetLogicAlarm alarm = InformationModel.MakeObject<CoT_NetLogicAlarm>("System Alarm: " + _autoIncrementID);
            alarm.id = _autoIncrementID;
            _autoIncrementID--;
            alarm.messageTextStatic = messageLocalized;
            alarm.messageType = (int)messageType;
            alarm.InputValue = (int)AlarmState.Set;
            if (e == null)
            {
                alarm.group = GetCallerInformation();
            }
            else
            {
                WriteStacktraceToCause(alarm, e);
            }
            if (logicObject != null)
            {
                WriteLogicObjectToCause(alarm, logicObject);
            }
            _systemAlarmsFolder.Children.Add(alarm);
            return alarm.id;
        }
    }



    private CoT_NetLogicAlarm WriteStacktraceToCause(CoT_NetLogicAlarm alarm, Exception e)
    {

        var causesRemedysObject = alarm.Children.Get("CausesRemedys");

        alarm.description = new LocalizedText(NodeId.InvalidNamespaceIndex, string.Empty, "Exception Message: " + e.Message, _actualLocaleId);

        CoT_CauseRemedy exceptionSource = InformationModel.MakeObject<CoT_CauseRemedy>("Source");
        exceptionSource.cause = new LocalizedText(NodeId.InvalidNamespaceIndex, string.Empty, $"Source: {e.Source}", _actualLocaleId);
        causesRemedysObject.Add(exceptionSource);

        CoT_CauseRemedy exceptionTypeName = InformationModel.MakeObject<CoT_CauseRemedy>("exceptionTypeName");
        exceptionTypeName.cause = new LocalizedText(NodeId.InvalidNamespaceIndex, string.Empty, $"exceptionTypeName: {e.GetType()}", _actualLocaleId);
        causesRemedysObject.Add(exceptionTypeName);

        if (e.InnerException != null)
        {
            CoT_CauseRemedy exceptionInnerException = InformationModel.MakeObject<CoT_CauseRemedy>("InnerException");
            exceptionInnerException.cause = new LocalizedText(NodeId.InvalidNamespaceIndex, string.Empty, $"InnerException: {e.InnerException}", _actualLocaleId);
            causesRemedysObject.Add(exceptionInnerException);
        }

        CoT_CauseRemedy exceptionStackTrace = InformationModel.MakeObject<CoT_CauseRemedy>("StackTrace");
        exceptionStackTrace.cause = new LocalizedText(NodeId.InvalidNamespaceIndex, string.Empty, $"StackTrace: {e.StackTrace}", _actualLocaleId);
        causesRemedysObject.Add(exceptionStackTrace);

        return alarm;
    }

    private CoT_NetLogicAlarm WriteLogicObjectToCause(CoT_NetLogicAlarm alarm, IUAObject logicObject)
    {
        var causesRemedysObject = alarm.Children.Get("CausesRemedys");

        CoT_CauseRemedy OptixPath = InformationModel.MakeObject<CoT_CauseRemedy>("OptixPath");
        OptixPath.cause = new LocalizedText(NodeId.InvalidNamespaceIndex, string.Empty, "OptixPath: {GetObjectPath(logicObject)}", _actualLocaleId);
        causesRemedysObject.Add(OptixPath);
        return alarm;
    }

    private static string GetObjectPath(IUANode node)
    {
        return node.Owner == null
            ? node.BrowseName
            : $"{GetObjectPath(node.Owner)}/{node.BrowseName}";
    }

    private string GetCallerInformation()
    {
        var stackTrace = new StackTrace(true);
        string stackTraceMessage = "";
        if (stackTrace.FrameCount > 2)
        {
            // The Calling Method is Frame 3
            var frame = stackTrace.GetFrame(3);

            string fileName = frame.GetFileName();
            string methodName = frame.GetMethod().Name;
            int lineNumber = frame.GetFileLineNumber();

            //Add StackTrace to string
            stackTraceMessage = "StackTrace: Filename: " + fileName + " Method: " + methodName + " LineNumber: " + lineNumber;
        }
        return stackTraceMessage;
    }

    private int CreateLogEntry(LocalizedText messageLocalized, MessageType messageType, Exception e, IUAObject logicObject, int alarmId)
    {
        lock (_LogLock)
        {
            StringBuilder logMessage = new();
            logMessage.Append($"Message: {messageLocalized.Text}");

            if (e == null)
            {
                logMessage.Append($"Caller: {GetCallerInformation()}");
            }
            else
            {
                logMessage = WriteStacktraceToString(logMessage, e);

            }
            if (logicObject != null)
            {
                WriteLogicObjectToString(logMessage, logicObject);
            }
            if (messageType == MessageType.None || messageType == MessageType.Info)
            {
                Log.Info("NetlogicsAlarm", logMessage.ToString());
            }
            if (messageType == MessageType.Warning || messageType == MessageType.Action)
            {
                Log.Warning("NetlogicsAlarm", logMessage.ToString());
            }
            if (messageType == MessageType.Fault || messageType == MessageType.CriticalFault)
            {
                Log.Error("NetlogicsAlarm", logMessage.ToString());
            }


            return alarmId;
        }
    }

    private StringBuilder WriteStacktraceToString(StringBuilder logMessage, Exception e)
    {
        logMessage.AppendLine($"Exception: {e.Message}");
        logMessage.AppendLine($"Source: {e.Source}");
        logMessage.AppendLine($"ExceptionTypeName: {e.GetType()}");
        if (e.InnerException != null)
        {
            logMessage.AppendLine($"InnerException: {e.InnerException}");
        }
        logMessage.AppendLine($"StackTrace: {e.StackTrace}");
        return logMessage;
    }

    private StringBuilder WriteLogicObjectToString(StringBuilder logMessage, IUAObject logicObject)
    {
        logMessage.AppendLine($"OptixPath: {GetObjectPath(logicObject)}");
        return logMessage;
    }


    private static void DeleteSystemAlarms()
    {
        //delete Alarms for not integrate them to remanent data
        IEnumerable<CoT_NetLogicAlarm> systemAlarms = _systemAlarmsFolder.GetNodesByType<CoT_NetLogicAlarm>();
        foreach (var alarm in systemAlarms)
        {
            alarm.InputValue = (int)AlarmState.Reset;
            alarm.Delete();
        }
        _systemAlarmsFolder.Children.Clear();
    }

    public static void ResetSystemAlarm(int alarmId)
    {
        //Reset one alarm
        IEnumerable<CoT_NetLogicAlarm> netLogicAlarms = _systemAlarmsFolder.GetNodesByType<CoT_NetLogicAlarm>();
        netLogicAlarms.First<CoT_NetLogicAlarm>((s) => s.id == alarmId).InputValue = (int)AlarmState.Reset;
    }

    public static Dictionary<MessageType, AlarmVisibilityObj> createAlarmVisibilities(ChildNodeCollection alarmVisibilityChildren)
    {
        Dictionary<MessageType, AlarmVisibilityObj> alarmVisibilities = new();

        foreach (CoT_AlarmVisibility alarmVisibility in alarmVisibilityChildren.OfType<CoT_AlarmVisibility>())
        {
            AlarmVisibilityObj alarmVisibilityObj = new AlarmVisibilityObj(alarmVisibility.showInSupressMode, alarmVisibility.showInSupressMode);
            alarmVisibilities.Add((MessageType)alarmVisibility.messageType, alarmVisibilityObj);
        }
        return alarmVisibilities;

    }
    private static bool ShouldSystemAlarmTriggert(MessageType messageType)
    {
        _alarmVisibilities.TryGetValue(messageType, out AlarmVisibilityObj alarmVisibilityObj);

        if (alarmVisibilityObj == null)
        {
            return true;
        }
        if (_SupressMode)
        {
            return alarmVisibilityObj.showInshowInSupressMode;
        }
        else
        {
            return alarmVisibilityObj.showInSupressMode;
        }
    }
}
public record AlarmVisibilityObj(bool showInSupressMode, bool showInshowInSupressMode);


