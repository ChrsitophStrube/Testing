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
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Linq;
using System.Collections.Generic;
using FTOptix.NativeUI;
using System.Threading;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using System.ComponentModel;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion


public class RT_CoT_SelectionInterfaceLogic : BaseNetLogic
{
	private IUAVariable _selectedOptionPointer;
	private IUAVariable _selectedID;

	public override void Start()
	{
		try
		{
			_selectedOptionPointer = GetVariable("selectedOption", LogicObject);
			_selectedID = GetVariable("selectedID", LogicObject);
			_selectedID.VariableChange += OnSelectedIdChanged;

			InitTyp dropdownInitializationMode = (InitTyp)GetVariable("dropdownInitializationMode", LogicObject).GetVariableValue<int>();
			Initialize(dropdownInitializationMode);
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("Dropdown Init Fault", MessageType.Warning, ex, LogicObject);
		}

	}

	private void Initialize(InitTyp initTyp)
	{
		if (InitTyp.ById == initTyp)
		{
			SelectOptionById(_selectedID.Value);
		}
		if (InitTyp.ByFirstElement == initTyp)
		{
			ResetSelectOptionToFirstElement();
		}
	}

	public override void Stop()
	{
		_selectedID.VariableChange -= OnSelectedIdChanged;
	}

	private void OnSelectedIdChanged(object sender, VariableChangeEventArgs e)
	{
		if (_selectedOptionPointer.Value==null|| e.NewValue != InformationModel.Get(_selectedOptionPointer.Value)?.GetVariable("id")?.Value)
		{
			SelectOptionById(e.NewValue);
		}
	}
	[ExportMethod]
	public void ResetSelectOptionToFirstElement()
	{
		try
		{
			List<CoT_SelectionOption> elements = Owner.Children.OfType<CoT_SelectionOption>().ToList();
			_selectedID.Value = elements.First().id;
			SelectOptionById(elements.First().id);
		}
		catch
		{
			Logger.CreateAlarmAndLog("First Element could not be selected",MessageType.Info);
		}
	}
	private void SelectOptionById(int id)
	{
		List<CoT_SelectionOption> elements = Owner.Children.OfType<CoT_SelectionOption>().ToList();
		foreach (var element in elements)
		{
			if (element.id == id)
			{
				_selectedOptionPointer.Value = element.NodeId;
				return;
			}
		}
		Logger.CreateAlarmAndLog( "Selected id is invalid", MessageType.Info);
		try
		{
			_selectedID.Value = InformationModel.Get<CoT_SelectionOption>(_selectedOptionPointer.Value).id;
		}
		catch
		{
			Logger.CreateAlarmAndLog("Selected Pointer is Empty", MessageType.Info);
			ResetSelectOptionToFirstElement();
		}
	}


}
