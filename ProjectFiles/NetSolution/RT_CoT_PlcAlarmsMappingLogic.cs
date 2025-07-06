#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.Alarm;
using System.Collections.Generic;
using System.Security.Claims;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using System.Linq;
using FTOptix.WebUI;
using FTOptix.OPCUAServer;
using FTOptix.Recipe;
using System.Threading;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;
using System.Data;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using CoT;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_PlcAlarmsMappingLogic : BaseNetLogic
{
	public static RT_CoT_PlcAlarmsMappingLogic ServerInstance;
	public IUAVariable StopReasonAlarm;

	IEnumerable<DigitalAlarm> _plcAlarms;
	Dictionary<int, DigitalAlarm> alarmsDictionary = new Dictionary<int, DigitalAlarm>();
	// NetLogic Configuration Properties
	TagStructure _alarmListPointerValue;
	string _nameIdArrayElement;
	string _nameDynTextElement;
	IUAVariable _alarmPollingTime;
	IUAVariable _firstStopReasonId;

	private RemoteVariableSynchronizer _variableSynchronizer;
	private int _eventsArrived;
	private LongRunningTask _asynchronousAlarmTask;
	private Boolean _keepTaskRunning = true;

	public override void Start()
	{
		Initialize();

		if (ServerInstance == null)
		{
			ServerInstance = this;
		}
		else
		{
			Logger.CreateAlarmAndLog("Server script instantiated twice");
		}
	}

	private void Initialize()
	{
		try
		{
			CreateAlarmDictionary();
			//Get alarm ID Array
			_nameIdArrayElement =  GetVariable("nameIdArrayElement", LogicObject).GetVariableValue<String>();
			//Get Dyn Text Array
			_nameDynTextElement =  GetVariable("nameDynTextElement", LogicObject).GetVariableValue<String>();
			//Get polling Time
			_alarmPollingTime = GetVariable("alarmPollingTime", LogicObject);
			// Create the VariableSynchronizer object UpdateTime 1 sec
			_variableSynchronizer = new RemoteVariableSynchronizer(new TimeSpan(TimeSpan.TicksPerMillisecond * _alarmPollingTime.Value));
			//Get alarmListPointer Value
			_alarmListPointerValue = GetPointer("alarmListPointer", LogicObject).GetPointedObj<TagStructure>();
			//Get single Id elements
			ChildNodeCollection alarmElements = _alarmListPointerValue.Children;
			foreach (TagStructure element in alarmElements)
			{
				//Array ID Element
				FTOptix.CommunicationDriver.Tag IdArrayElement = (FTOptix.CommunicationDriver.Tag)element.Children.Get(_nameIdArrayElement);
				_variableSynchronizer.Add(IdArrayElement);
				IdArrayElement.VariableChange += (sender, e) => _eventsArrived = 1;
				//Array DynTextElement
				FTOptix.CommunicationDriver.Tag DynTextElement = (FTOptix.CommunicationDriver.Tag)element.Children.Get(_nameDynTextElement);
				_variableSynchronizer.Add(DynTextElement);
			}

			_firstStopReasonId = GetVariable("StopReasonAlarmId", LogicObject);
			_variableSynchronizer.Add(_firstStopReasonId);
			_firstStopReasonId.VariableChange += UpdateStopReasonAlarm;
			StopReasonAlarm = GetVariable("StopReasonAlarm", LogicObject);

			_asynchronousAlarmTask = new LongRunningTask(AwaitEventsAndTriggerAlarms, LogicObject);
			_asynchronousAlarmTask.Start();
			//let the alarms comparison run at least once at startup
			_eventsArrived = 1;
			Log.Info("Alarm handling start done");
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("Alarming Faulted", MessageType.Warning, ex, LogicObject);
		}

	}

	private void CreateAlarmDictionary()
	{

		// get all alarms in alarm folder
		//Folder AlarmsFolder = (Folder)Project.Current.Get("Alarms");
		_plcAlarms = Project.Current.FindNodesByType<CoT_PlcAlarm>();

		// reset all alarms at startup
		ResetAllAlarms();

		foreach (DigitalAlarm alarm in _plcAlarms)
		{
			// skip alarm if it already exists
			if (!this.alarmsDictionary.TryAdd(alarm.GetVariable("id").Value, alarm))
			{
				Log.Warning("MyHMI_AlarmFault", "Alarm: " + alarm.BrowseName + " is existing multiple times");
				continue;
			}
		}
	}
	public override void Stop()
	{
		// reset all alarms at Stop of Runtime
		ResetAllAlarms();
		_variableSynchronizer?.Dispose();
		_keepTaskRunning = false;
		_asynchronousAlarmTask?.Dispose();
	}

	private void AwaitEventsAndTriggerAlarms()
	{
		try
		{
			while (_keepTaskRunning)
			{
				if (Interlocked.Exchange(ref _eventsArrived, 0) == 1)
				{
					CompareAlarmArrays();
					UpdateStopReasonAlarm(null, EventArgs.Empty);
				}
				CheckStopReasonPresent();
				Thread.Sleep(_alarmPollingTime.Value);
			}
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("Alarm Thread Exception.Alarming Is Stopped", MessageType.Warning, ex, LogicObject);
		}
	}

	List<AlarmElement> ArrayElementsListOld = new List<AlarmElement>();
	private void CompareAlarmArrays()
	{
		ChildNodeCollection alarmElements = _alarmListPointerValue.Children;

		// Create List from array elements
		List<AlarmElement> ArrayElementsList = alarmElements
	   .Select(i => new AlarmElement
	   {
		   Id = (int)((FTOptix.CommunicationDriver.Tag)i.Children.Get(_nameIdArrayElement)).Value,
		   DynText = (string)((FTOptix.CommunicationDriver.Tag)i.Children.Get(_nameDynTextElement)).Value
	   })
	   .ToList();

		//Check Alarm twice in List
		ArrayElementsList
		.Where(i => i.Id != 0)
		.GroupBy(i => i.Id)
		.Where(i => i.Count() > 1)
		.ToList()
		.ForEach(i => Logger.CreateAlarmAndLog("Alarm with id: " + i.Key + " is already triggered"));

		// Reset Alarms
		ArrayElementsListOld
			.Where(i => i.Id != 0)
			.Where(i => !ListContainsId(ArrayElementsList, i.Id))
			.ToList<AlarmElement>()
			.ForEach(i => ResetAlarm(i.Id));

		//Set Alarms
		ArrayElementsList
			 .Where(i => i.Id != 0)
			 .Where(i => !ListContainsId(ArrayElementsListOld, i.Id))
			 .ToList<AlarmElement>()
			 .ForEach(i => SetAlarm(i.Id, i.DynText));

		ArrayElementsListOld = new List<AlarmElement>(ArrayElementsList);
	}

	private bool ListContainsId(List<AlarmElement> alarmElementsList, int Id)
	{
		foreach (AlarmElement alarmElement in alarmElementsList)
		{
			if (alarmElement.Id == Id)
			{
				return true;
			}
		}

		return false;
	}

	private void SetAlarm(int alarmId, string dynText)
	{
		DigitalAlarm alarmToChange;
		//Check if alarm exists and get alarm
		alarmsDictionary.TryGetValue(alarmId, out alarmToChange);

		if (alarmToChange == null)
		{
			//createSystemAlarm
			Logger.CreateAlarmAndLog("Alarm: " + alarmId + " is not existing in the Project");
			return;
		}

		if (!string.IsNullOrEmpty(dynText))
		{
			alarmToChange.GetVariable("messageTextDynamic").Value = dynText;
		}
		alarmToChange.InputValue = (int)AlarmState.Set;
	}


	private void ResetAlarm(int alarmId)
	{
		DigitalAlarm alarmToChange;
		//Check if alarm exists and get alarm
		alarmsDictionary.TryGetValue(alarmId, out alarmToChange);

		if (alarmToChange == null)
		{
			Logger.CreateAlarmAndLog("Alarm: " + alarmId + " Could not be reset");
			return;
		}

		alarmToChange.InputValue = (int)AlarmState.Reset;
	}



	private void ResetAllAlarms()
	{
		if (_plcAlarms != null)
		{
			foreach (DigitalAlarm alarm in _plcAlarms)
			{
				alarm.InputValue = (int)AlarmState.Reset;
			}
		}
	}

	private void UpdateStopReasonAlarm(object sender, EventArgs e)
	{
		DigitalAlarm stopReasonAlarm;

		// If some StopReasonId is set.
		if (_firstStopReasonId.Value)
		{
			// Check if the StopReasonId exists in the alarms dictionary.
			alarmsDictionary.TryGetValue(_firstStopReasonId.Value, out stopReasonAlarm);
			// If the StopReasonAlarm does not exist in the alarms dictionary.
			if (stopReasonAlarm == null)
			{
				Logger.CreateAlarmAndLog("Stop Reason Alarm with id:" + _firstStopReasonId.Value + "is not existing in the Project");
				StopReasonAlarm.Value = NodeId.Empty;
			}
			// If the StopReasonAlarm exists in the alarms dictionary.
			else
			{
				StopReasonAlarm.Value = stopReasonAlarm.NodeId;
			}
		}
		// If no StopReasonId is set.
		else
		{
			StopReasonAlarm.Value = NodeId.Empty;
		}
		return;
	}

	private void CheckStopReasonPresent()
	{
		DigitalAlarm StopReasonAlarm;
		alarmsDictionary.TryGetValue(_firstStopReasonId.Value, out StopReasonAlarm);
		if (StopReasonAlarm == null)
		{
			return;
		}
		if (StopReasonAlarm.InputValue == (int)AlarmState.Reset)
		{
			this.StopReasonAlarm.Value = NodeId.Empty;
		}
	}
}


public class AlarmElement
{
	public int Id { get; set; }
	public string DynText { get; set; }
}

