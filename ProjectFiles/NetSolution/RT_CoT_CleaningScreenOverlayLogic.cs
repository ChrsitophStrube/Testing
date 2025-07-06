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
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Threading;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_CleaningScreenOverlayLogic : BaseNetLogic
{
	private IUAVariable _duration;
	private IUAVariable _remainingTime;
	private LongRunningTask _timer;
	private bool _stopCountdown = false;
	public override void Start()
	{
		_duration = LogicObject.GetVariable("duration");
		_remainingTime = LogicObject.GetVariable("remainingTime");
		_remainingTime.Value = _duration.Value;
		_timer = new LongRunningTask(Countdown, LogicObject);
		_timer.Start();
	}

	public override void Stop()
	{
		_stopCountdown = true;
		_timer.Dispose();
	}
	
	private void Countdown()
	{
		while (_remainingTime.Value > 0 && !_stopCountdown)
		{
			Thread.Sleep(1000);
			_remainingTime.Value--;
		}
	}
}
