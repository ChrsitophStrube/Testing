#region Using directives
using System;
using CoreBase = FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using FTOptix.UI;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using CoT;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
#endregion

public class ClockLogic : BaseNetLogic
{
	public override void Start()
	{
		periodicTask = new PeriodicTask(UpdateTime, 1000, LogicObject);
		periodicTask.Start();
	}

	public override void Stop()
	{
		periodicTask.Dispose();
		periodicTask = null;
	}

	private void UpdateTime()
	{
		try
		{
			DateTime localTime = DateTime.Now;
			DateTime utcTime = DateTime.UtcNow;

			GetVariable("Time", LogicObject).SetVariableValue<DateTime>(localTime);
			GetVariable("Time/Year", LogicObject).SetVariableValue<int>(localTime.Year);
			GetVariable("Time/Month", LogicObject).SetVariableValue<int>(localTime.Month);
			GetVariable("Time/Day", LogicObject).SetVariableValue<int>(localTime.Day);
			GetVariable("Time/Hour", LogicObject).SetVariableValue<int>(localTime.Hour);
			GetVariable("Time/Minute", LogicObject).SetVariableValue<int>(localTime.Minute);
			GetVariable("Time/Second", LogicObject).SetVariableValue<int>(localTime.Second);

			GetVariable("UTCTime", LogicObject).SetVariableValue<DateTime>(utcTime);
			GetVariable("UTCTime/Year", LogicObject).SetVariableValue<int>(utcTime.Year);
			GetVariable("UTCTime/Month", LogicObject).SetVariableValue<int>(utcTime.Month);
			GetVariable("UTCTime/Day", LogicObject).SetVariableValue<int>(utcTime.Day);
			GetVariable("UTCTime/Hour", LogicObject).SetVariableValue<int>(utcTime.Hour);
			GetVariable("UTCTime/Minute", LogicObject).SetVariableValue<int>(utcTime.Minute);
			GetVariable("UTCTime/Second", LogicObject).SetVariableValue<int>(utcTime.Second);
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("Clock logic Faulted", MessageType.Warning, ex, LogicObject);
		}


	}

	private PeriodicTask periodicTask;
}
