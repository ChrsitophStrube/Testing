#region Using directives
using System;
using System.Xml;
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
using System.Collections.Generic;
using FTOptix.SQLiteStore;
using FTOptix.System;
using System.Linq;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;
using System.Threading;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.TwinCAT;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
#endregion

public class RT_CoT_NavigationLogic : BaseNetLogic //File Naming RT_CoT_...
{
	#region Parameters
	public List<CoT_NavColumnTemplate> Columns;
	public PanelLoader ContentPanelLoader;
	public IUAVariable ScreenSize;

	private List<TreeNode<NavItem>> _roots;
	private ResourceUri _xmlPath;
	private IUAVariable _navigationPath;
	private IUAVariable _openScreenName;
	private RowLayout _columnLayout;
	private NavHistory _navHistory;
	private IUAVariable _canGoBackInHistory;
	private IUAVariable _canGoForwardInHistory;
	private NavItem _currentOpenScreen;
	private Rectangle _backgroundOverlay;
	private Panel _sideBar;
	private IUAVariable _isOpen;
	private NavItem _highlightedItem;
	private object _highlightLock = new();
	#endregion

	public override void Start()
	{
		ContentPanelLoader = GetAliasObject<PanelLoader>("contentPanelLoader", LogicObject);
		_navigationPath = GetVariable("navigationPath", LogicObject);
		_openScreenName = GetVariable("openScreenName", LogicObject);
		_canGoBackInHistory = GetVariable("canGoBackInHistory", LogicObject);
		_canGoForwardInHistory = GetVariable("canGoForwardInHistory", LogicObject);
		_xmlPath = new ResourceUri(GetVariable("xmlPath", LogicObject).GetVariableValue<string>());
		if (_xmlPath == null)
		{
			throw new Exception("NavigationContent.xml not found!");
		}
		_columnLayout = GetPointer("columnLayout", LogicObject).GetPointedObj<RowLayout>();
		Columns =
		[
			GetPointer("column1", LogicObject).GetPointedObj<CoT_NavColumnTemplate>(),
			GetPointer("column2", LogicObject).GetPointedObj<CoT_NavColumnTemplate>(),
			GetPointer("column3", LogicObject).GetPointedObj<CoT_NavColumnTemplate>(),
			GetPointer("column4", LogicObject).GetPointedObj<CoT_NavColumnTemplate>(),
			GetPointer("column5", LogicObject).GetPointedObj<CoT_NavColumnTemplate>(),
		];
		_backgroundOverlay = GetPointer("backgroundOverlay", LogicObject).GetPointedObj<Rectangle>();
		_sideBar = GetPointer("sideBar", LogicObject).GetPointedObj<Panel>();
		ScreenSize = GetVariable("screenSize", LogicObject);
		ScreenSize.VariableChange += OnScreenSizeChanged;
		_isOpen = GetVariable("isOpen", LogicObject);
		_isOpen.VariableChange += OnIsOpenChanged;
		_navHistory = new NavHistory();

		InitializeNavigation();
	}

	public override void Stop()
	{
		ScreenSize.VariableChange -= OnScreenSizeChanged;
		_isOpen.VariableChange -= OnIsOpenChanged;
	}

	#region Callback Functions
	private void OnScreenSizeChanged(object sender, VariableChangeEventArgs e)
	{
		DisplayNavigation(false);
	}

	private void OnIsOpenChanged(object sender, VariableChangeEventArgs e)
	{
		DisplayNavigation(e.NewValue);
	}
	#endregion

	#region Export Methods

	/// <summary>
	/// Upadte the navigation if the xml changed.
	/// </summary>
	[ExportMethod]
	public void UpdateNavigation()
	{
		InitializeNavigation();
	}

	/// <summary>
	/// Open a specific screen. Used for Quick-Navigation-Buttons, Favorites etc.
	/// </summary>
	/// <param name="screen">Id of the screen that should be opened</param>
	[ExportMethod]
	public void OpenScreen(NodeId screen)
	{
		try
		{
			if (screen.IsEmpty)
			{
				throw new Exception($"Screen {screen} does not exist!");
			}
			NavItem item = FindNavItem(screen);
			if (item == null)
			{
				ContentPanelLoader.ChangePanel(screen);
			}
			else
			{
				OpenScreenWithHistory(item);
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in OpenScreen: {ex.Message}");
		}
	}

	/// <summary>
	/// Open a specific screen. Used for Quick-Navigation-Buttons, Favorites etc.
	/// </summary>
	/// <param name="nodePath">NodePath to the screen in the Optix project. (Example: "UI/ExampleData/VisibleScreen")</param>
	[ExportMethod]
	public void OpenScreenWithPath(string nodePath)
	{
		try
		{
			NodeId screenId = Project.Current.Get(nodePath).NodeId;
			OpenScreen(screenId);
		}
		catch (Exception ex)
		{
			Log.Error($"Error in OpenScreen: {ex.Message}");
		}
	}

	/// <summary>
	/// Return to the last screen in the navigation history
	/// </summary>
	[ExportMethod]
	public void GoBackInHistory()
	{
		try
		{
			if (_navHistory.CanGoBack())
			{
				OpenScreenInternal(_navHistory.GoBack());
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error in GoForwardInHistory: " + ex.Message);
		}
	}

	/// <summary>
	/// Go to the next screen in the navigation history
	/// </summary>
	[ExportMethod]
	public void GoForwardInHistory()
	{
		try
		{
			if (_navHistory.CanGoForward())
			{
				OpenScreenInternal(_navHistory.GoForward());
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error in GoForwardInHistory: " + ex.Message);
		}
	}

	/// <summary>
	/// Closes the last open branch. Used for small screens where only one column is visible to go back to the last column.
	/// </summary>
	[ExportMethod]
	public void CloseLastLevel()
	{
		foreach (TreeNode<NavItem> root in _roots)
		{
			if (root.Value.IsOpen)
			{
				var openVisibleDescendants = root.GetDescendantsValues().Where(elem => elem.IsOpen && elem.IsVisible).ToList();
				if (openVisibleDescendants.Count > 0)
				{
					openVisibleDescendants.OrderBy(elem => elem.TreeNode.GetTreeLevel()).Last().Close();
				}
				else
				{
					root.Value.Close();
				}
			}
		}
	}
	#endregion

	#region Helper Fuctions

	private void InitializeNavigation()
	{
		try
		{
			Columns.ForEach(column
			=> column.Find("Layout").Children.Where(child
			=> child is CoT_NavItem).ToList().ForEach(elem
			=> elem.Delete()));
			_navHistory.ClearHistory();

			NavXmlReader reader = new();
			_roots = reader.ReadXml(this, _xmlPath);

			foreach (TreeNode<NavItem> root in _roots)
			{
				root.Value.UpdateVisibility();
			}
			UpdateColumnVisibility();
			DisplayNavigation(false);
		}
		catch (Exception ex)
		{
			Log.Error("Error in ReadXML: " + ex.Message);
		}
	}

	/// <summary>
	/// Hide or show the navgation.
	/// </summary>
	/// <param name="display">True = show naviagtion, False = hide navigaiton.</param>
	public void DisplayNavigation(bool display)
	{
		if (ScreenSize.Value == 2)
		{
			_sideBar.Visible = true;
			Columns[0].Find("Layout").Children.Where(child
			=> child is CoT_NavItem).ToList().ForEach(elem
			=> GetVariable("iconOnly", (IUAObject)elem).Value = !display);
			ChangeSideBarWidth(display ? 312 : 96);
			if (!display)
			{
				_roots.ForEach(elem => elem.Value.Close());
			}
		}
		else
		{
			Columns[0].Find("Layout").Children.Where(child
			=> child is CoT_NavItem).ToList().ForEach(elem
			=> GetVariable("iconOnly", (IUAObject)elem).Value = true);
			ChangeSideBarWidth(96);
			_sideBar.Visible = display;
		}
		_columnLayout.Visible = display;
		_backgroundOverlay.Visible = display;
		_isOpen.Value = display;
		if (display)
		{
			HighlightScreenPath();
		}
	}

	/// <summary>
	/// Change the width of the 1st navigation column
	/// </summary>
	/// <param name="width">New width</param>
	public void ChangeSideBarWidth(int width)
	{
		IUAVariable columnsLeftMargin = GetVariable("leftMargin", _columnLayout);
		IUAVariable sideBarWidth = GetVariable("width", _sideBar);
		sideBarWidth.Value = width;
		columnsLeftMargin.Value = width;
	}

	/// <summary>
	/// Add a new column to the navigation.
	/// </summary>
	/// <param name="navLevel">Navigation tree depth where the column should be added.</param>
	public void AddNavigationColumn(int navLevel)
	{
		try
		{
			foreach (var column in Columns)
			{
				if (column.BrowseName == $"Column{navLevel}")
				{
					return;
				}
			}
			var newColumn = InformationModel.Make<CoT_NavColumnTemplate>($"Column{navLevel}");
			_columnLayout.Add(newColumn);
			Columns.Add(newColumn);
		}
		catch (Exception ex)
		{
			Log.Error("Error in AddNavigationColumn: " + ex.Message);
		}
	}

	/// <summary>
	/// Open a screen in the content panel.
	/// </summary>
	/// <param name="item">NavItem that corresponds to the screen that should be opened.</param>
	public void OpenScreenWithHistory(NavItem item)
	{
		try
		{
			OpenScreenInternal(item);
			_navHistory.OpenScreen(item);
			UpdateNavHistoryValues();
		}
		catch (Exception ex)
		{
			Log.Error("Error in OpenScreenWithHistory: " + ex.Message);
		}
	}

	private void OpenScreenInternal(NavItem item)
	{
		try
		{
			if (item.ScreenOpenId == null)
			{
				Log.Error($"Cannot open {item.Title}! Has no connected Screen!");
			}

			if (ContentPanelLoader != null)
			{
				_currentOpenScreen?.Close();
				item.Open();
				_currentOpenScreen = item;
				_navigationPath.Value = item.TreeNode.Parent?.Value.GetNavPath();
				_openScreenName.Value = item.Title;
				DisplayNavigation(false);
				ContentPanelLoader.ChangePanel(item.ScreenOpenId);
				UpdateNavHistoryValues();
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error in OpenScreenInternal: " + ex.Message);
		}
	}

	/// <summary>
	/// Check if a column has visible elements and show or hide it depending on the result
	/// </summary>
	public void UpdateColumnVisibility()
	{
		try
		{
			for (int i = 1; i < Columns.Count; i++)
			{
				bool visibleElements = false;
				foreach (var element in Columns[i].Find("Layout").Children.Where(child => child is CoT_NavItem))
				{
					if (((CoT_NavItem)element).Visible)
					{
						visibleElements = true;
						break;
					}
				}
				if (ScreenSize.Value != 2 && i > 1)
				{
					if (visibleElements)
					{
						Columns[i - 1].Visible = false;
					}
				}
				Columns[i].Visible = visibleElements;
			}
		}
		catch (Exception ex)
		{
			Log.Error("Error in UpdateColumnVisibility: " + ex.Message);
		}
	}

	/// <summary>
	/// Get the Optix object that is connected to the NetLogic.
	/// </summary>
	/// <returns>Optix NetLogic Node</returns>
	public IUANode GetNetlogicObject()
	{
		return LogicObject;
	}

	/// <summary>
	/// Close the other roots of the navigation when opening a root
	/// </summary>
	/// <param name="root">Root that is being opened</param>
	public void CloseOtherRoots(TreeNode<NavItem> root)
	{
		_roots.FindAll(elem => elem != root).ForEach(elem => elem.Value.Close());
	}

	private void UpdateNavHistoryValues()
	{
		try
		{
			_canGoBackInHistory.Value = _navHistory.CanGoBack();
			_canGoForwardInHistory.Value = _navHistory.CanGoForward();
		}
		catch (Exception ex)
		{
			Log.Error("Error in UpdateHistoryValues: " + ex.Message);
		}
	}

	private NavItem FindNavItem(NodeId ScreenId)
	{
		try
		{
			NavItem item = null;
			foreach (var root in _roots)
			{
				item = root.Value.FindNavItem(ScreenId);
				if (item != null)
				{
					return item;
				}
			}
			return item;
		}
		catch (Exception ex)
		{
			Log.Error("Error in FindNavItem: " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// Highlight the current screen path as described in style guide
	/// </summary>
	public void HighlightScreenPath()
	{
		try
		{
			lock (_highlightLock)
			{
				NavItem nextHighlightedItem;
				if (_currentOpenScreen != null)
				{
					List<TreeNode<NavItem>> currentScreenPath = _currentOpenScreen.TreeNode.GetTreePath();
					NavItem temp = null;

					foreach (var root in _roots)
					{
						if (root.Value.IsOpen)
						{
							List<NavItem> openVisibleDescendants = root.GetDescendantsValues().Where(item => item.IsOpen && item.IsVisible).ToList();
							if (openVisibleDescendants.Count > 0)
							{
								temp = openVisibleDescendants.OrderBy(item => item.TreeNode.GetTreeLevel()).Last();
							}
							else
							{
								temp = root.Value;
							}
							break;
						}
					}
					if (temp == null)
					{

						nextHighlightedItem = currentScreenPath.First()?.Value;
					}
					else if (_currentOpenScreen == temp)
					{
						nextHighlightedItem = temp;
					}
					else
					{
						List<TreeNode<NavItem>> currentNavPath = temp.TreeNode.GetTreePath();
						while (currentNavPath.Count > 0 && currentScreenPath.Count > 0 && currentScreenPath.First() == currentNavPath.First())
						{
							currentScreenPath = currentScreenPath.Skip(1).ToList();
							currentNavPath = currentNavPath.Skip(1).ToList();
						}
						nextHighlightedItem = currentScreenPath.First()?.Value;
					}
					if (nextHighlightedItem != _highlightedItem)
					{
						_highlightedItem?.Highlight(false);
						_highlightedItem = nextHighlightedItem;
						_highlightedItem.Highlight(true);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavigationLogic:HighlightScreenPath: {ex.Message}");
		}
	}

	public void SetColumnHeaderText(LocalizedText text, int column)
	{
		if (column < Columns.Count)
		{
			GetVariable("headerText", Columns[column]).Value = text;
		}
	}
	#endregion
}
