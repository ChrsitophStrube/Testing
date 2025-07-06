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
using System.Collections.Generic;
using System.Linq;
using FTOptix.AuditSigning;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_HeaderLogic : BaseNetLogic
{
	private IUANode _headerbar;
	private IUAVariable _iconOnlySize;
	private IUAVariable _minSize;
	private IUAVariable _mandatoryElementsSize;
	private IUAVariable _mainWindowWidth;
	private List<ResponsiveHeaderItem> _headerItems;
	private IUAVariable _furtherFunctionsMenuNodeId;
	public override void Start()
	{
		try
		{
			_headerbar = GetPointer("headerBar", LogicObject).GetPointedObj<IUANode>();
			_headerItems = new();
			_iconOnlySize = GetVariable("iconOnlySize", LogicObject);
			_minSize = GetVariable("minSize", LogicObject);
			_mandatoryElementsSize = GetVariable("mandatoryElemetsSize", LogicObject);
			_mainWindowWidth = GetVariable("mainWindowWidth", LogicObject);
			_furtherFunctionsMenuNodeId = GetVariable("furtherFunctionsMenu", LogicObject);
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("HeaderBar Init Fault", MessageType.Warning, ex, LogicObject);
		}
		foreach (var item in _headerbar.FindNodesByType<CoT_CTRL_HeaderItem>().Where(elem => GetVariable("visibility", elem).Value))
		{
			_headerItems.Add(new ResponsiveHeaderItem(item, _minSize.Value, _iconOnlySize.Value));
		}
		_mainWindowWidth.VariableChange += OnScreenWidthChanged;
		_furtherFunctionsMenuNodeId.VariableChange += OnFFMenuOpened;
		AdjustHeaderItems();
	}

	private void OnScreenWidthChanged(object sender, VariableChangeEventArgs e)
	{
		AdjustHeaderItems();
	}

	private void OnFFMenuOpened(object sender, VariableChangeEventArgs e)
	{
		IUANode ffMenu = InformationModel.Get(e.NewValue);
		ffMenu.GetNodesByType<CoT_FurtherFunctionsMenuItem>().ToList().ForEach(elem => elem.Delete());
		foreach (var item in _headerItems.FindAll(elem => !elem.Visible))
		{
			ffMenu.Add(item.GetFurtherFunctionsItem());
		}
	}

	public override void Stop()
	{
		_mainWindowWidth.VariableChange -= OnScreenWidthChanged;
		_furtherFunctionsMenuNodeId.VariableChange -= OnFFMenuOpened;
	}

	private void AdjustHeaderItems()
	{
		int neededSize = CalculateNeededSize();
		int diff = _mainWindowWidth.Value - neededSize;
		bool result;
		if (diff < 0)
		{
			result = MakeItemsSmaller();
		}
		else
		{
			result = MakeItemsBigger(diff);
		}
		if (result)
		{
			AdjustHeaderItems();
		}
	}

	private bool MakeItemsSmaller()
	{
		var visibleItems = _headerItems.FindAll(elem => elem.Visible);
		if (visibleItems.Count > 0)
		{
			var shrinkableItems = visibleItems.FindAll(elem => elem.CanShrink && !elem.Shrunk);
			if (shrinkableItems.Count > 0)
			{
				GetItemWithLowestPrio(shrinkableItems).Shrink();
			}
			else
			{
				GetItemWithLowestPrio(visibleItems).Hide();
			}
			return true;
		}
		return false;
	}

	private bool MakeItemsBigger(int availableSpace)
	{
		var hiddenItems = _headerItems.FindAll(elem => !elem.Visible);
		if (hiddenItems.Count > 0)
		{
			var item = GetItemWithHighestPrio(hiddenItems);
			if (item.CanShow(availableSpace))
			{
				item.Show();
				return true;
			}
			return false;
		}
		else
		{
			var shrunkItems = _headerItems.FindAll(elem => elem.Shrunk);
			if (shrunkItems.Count > 0)
			{
				var item = GetItemWithHighestPrio(shrunkItems);
				if (item.CanExpand(availableSpace))
				{
					item.Expand();
					return true;
				}
			}
			return false;
		}
	}

	private ResponsiveHeaderItem GetItemWithLowestPrio(List<ResponsiveHeaderItem> items)
	{
		return items.OrderBy(elem => elem.Priority).ToList()[0];
	}

	private ResponsiveHeaderItem GetItemWithHighestPrio(List<ResponsiveHeaderItem> items)
	{
		return items.OrderByDescending(elem => elem.Priority).ToList()[0];
	}

	private int CalculateNeededSize()
	{
		int neededSize = _mandatoryElementsSize.Value;
		foreach (var item in _headerItems)
		{
			neededSize += item.GetSize();
		}
		return neededSize;
	}
}
