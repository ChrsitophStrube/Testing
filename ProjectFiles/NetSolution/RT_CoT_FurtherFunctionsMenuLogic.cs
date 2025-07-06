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
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_FurtherFunctionsMenuLogic : BaseNetLogic
{
	private IUAVariable _headerItemIsOpen;
	public override void Start()
	{
		InformationModel.GetVariable(LogicObject.GetVariable("headerLogicVariable").Value).Value = LogicObject.GetVariable("headerItemsMenu").Value;
		_headerItemIsOpen = LogicObject.GetVariable("headerItemIsOpen");
		_headerItemIsOpen.Value = true;
	}

	public override void Stop()
	{
		_headerItemIsOpen.Value = false;
	}
}
