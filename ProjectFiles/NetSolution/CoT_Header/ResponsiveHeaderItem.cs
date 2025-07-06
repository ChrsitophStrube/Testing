#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.Alarm;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.EventLogger;
using FTOptix.Store;
using FTOptix.Recipe;
using System.Linq;
using System.Timers;
using System.Collections.Generic;
using System.Collections;
using System.Threading;
using System.Xml;
using FTOptix.SQLiteStore;
using UAManagedCore.OpcUa;
#endregion

/// <summary>
/// This class represents a CoT_CTRL_HeaderItem and is optimized to create the responsive behavior of the header bar
/// </summary>
public class ResponsiveHeaderItem
{
	public bool Visible
	{
		get => _visible.Value;
	}
	public bool Shrunk
	{
		get => _shrunk.Value;
	}
	public int Priority { get; }
	public bool CanShrink { get; }
	public IUANode Item;

	private IUAVariable _visible;
	private int _minSize;
	private int _shrunkSize;
	private IUAVariable _shrunk;

	public ResponsiveHeaderItem(IUANode item, int minSize, int shrunkSize)
	{
		Item = item;
		_minSize = minSize;
		_shrunkSize = shrunkSize;
		_shrunk = item.GetVariable("internals").GetVariable("shrunk");
		Priority = item.GetVariable("priority").Value;
		CanShrink = item.GetVariable("canShrink").Value;
		_visible = item.GetVariable("internals").GetVariable("tempVisibility");
	}

	public int GetSize()
	{
		return Visible ? (Shrunk ? _shrunkSize : _minSize) : 0;
	}

	public void Hide()
	{
		_visible.Value = false;
	}

	public bool CanShow(int availableSize)
	{
		return availableSize > (Shrunk ? _shrunkSize : _minSize);
	}

	public void Show()
	{
		_visible.Value = true;
	}

	public void Shrink()
	{
		_shrunk.Value = true;
	}

	public bool CanExpand(int availableSize)
	{
		return availableSize > _minSize - _shrunkSize;
	}

	public void Expand()
	{
		_shrunk.Value = false;
	}

	public CoT_FurtherFunctionsMenuItem GetFurtherFunctionsItem()
	{
		string text = Item.GetVariable("text").Value;
		var ffItem = InformationModel.Make<CoT_FurtherFunctionsMenuItem>(text);
		ffItem.GetVariable("enable").SetDynamicLink(Item.GetVariable("enable"));
		ffItem.GetVariable("text").SetDynamicLink(Item.GetVariable("text"));
		ffItem.GetVariable("icon").SetDynamicLink(Item.GetVariable("icon"));
		ffItem.GetVariable("screen").SetDynamicLink(Item.GetVariable("screen"));
		ffItem.GetVariable("userRole").SetDynamicLink(Item.GetVariable("userRole"));
		return ffItem;
	}
}
