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
using static RT_CoT_Helper;
#endregion

/// <summary>
/// A class that represents a single item in the navigation tree structure
/// </summary>
public class NavItem
{
	#region Parameters
	/// Actual object parameters
	public NodeId ScreenOpenId { get; } // is null for all entries that don't open a screen (root and branches)
	public LocalizedText Title { get; }
	public ResourceUri Icon { get; }
	public bool HasChildren
	{
		get => _navItem.GetVariable("hasChildren").Value;
		set => _navItem.GetVariable("hasChildren").Value = value;
	}
	public bool IsVisible => _visible.Value;
	public bool IsOpen => _isOpen.Value;

	/// Tree structure
	public TreeNode<NavItem> TreeNode { get; }

	/// References
	private RT_CoT_NavigationLogic _navLogic;
	private CoT_NavItem _navItem { get; set; } // is null for the root object
	private IUAVariable _isOpen;
	private IUAVariable _visible;
	private IUAVariable _visibilityRestriction;
	private IUAVariable _whiteBarVisible;
	private IUAVariable _isWizard;
	private string _navPath = "";
	#endregion

	#region Constructor

	/// <summary>
	/// Constructor for leafs and branches in the navigation tree
	/// </summary>
	/// <param name="NavLogic">RT_CoT_NavigationLogic class that uses the built navigation tree</param>
	/// <param name="TreeNode">Parent node in the navigation tree</param>
	/// <param name="IconPath">Project folder relative path to the icon file</param>
	/// <param name="Title">Language key that is used for labeling the object in the navigation</param>
	/// <param name="ScreenPath">Screenpath is the project-relative-path to the screen that should be opened (Example: "UI/ExampleData/SampleScreen"). This is only needed for leafs not for branches.</param>
	/// <param name="VisibiltyRestriction">Name of the parameter that is used for restriction</param>
	/// <param name="IsWizard">Boolean whether this opens a wizard or not</param>
	public NavItem(RT_CoT_NavigationLogic NavLogic, TreeNode<NavItem> TreeNode, string IconPath, string Title, string ScreenPath = "")
	{
		try
		{
			_navLogic = NavLogic;
			this.TreeNode = TreeNode;

			if (IconPath != "")
			{
				Icon = ResourceUri.FromProjectRelativePath(IconPath);
			}

			/// If there is no translation found for the title, the title (language key) is shown as text
			this.Title = InformationModel.LookupTranslation(new LocalizedText(Title));
			if (this.Title.Text == "" && this.Title.TextId == "")
			{
				this.Title = new LocalizedText(Title);
				this.Title.Text = Title;
			}

			/// Create Optix object
			GenerateOptixObject();

			/// Screenpath needs to be used to find the actual screen to open.
			/// This is only needed for leafs not for branches
			/// Screenpath is the project-relative-path without the project name in front. (Example: "UI/ExampleData/SamplePanel")
			if (ScreenPath != "")
			{
				IUANode screen = Project.Current.Get(ScreenPath);
				if (screen != null)
				{
					ScreenOpenId = screen.NodeId;
				}
				else
				{
					Log.Warning("Panel " + ScreenPath + " does not exist!");
				}

				string VisibilityRestriction = screen.GetVariable("navigationRestriction") != null ? screen.GetVariable("navigationRestriction").Value : String.Empty;

				if (VisibilityRestriction != String.Empty)
				{
					_visibilityRestriction = _navLogic.GetNetlogicObject().Owner.GetVariable(_navLogic.GetNetlogicObject().GetVariable("RestrictionsName").Value).GetVariable(VisibilityRestriction);
					_visibilityRestriction.VariableChange += ScreenVisibilityChanged;
				}

				_isWizard.Value = screen.GetVariable("isGuidedWorkflow") != null ? screen.GetVariable("isGuidedWorkflow").Value : false;
			}
			else
			{
				_isWizard.Value = false;
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:Constructor2: {ex.Message}");
		}
	}

	~NavItem()
	{
		try
		{
			if (_navItem != null)
			{
				_navItem.FindByType<Button>().OnMouseUp -= OnClick;
			}
			if (_visibilityRestriction != null)
			{
				_visibilityRestriction.VariableChange -= ScreenVisibilityChanged;
			}
		}
		catch (Exception ex)
		{
			Log.Verbose1($"Error in NavItem:Destructor: {ex.Message}");
		}
	}
	#endregion

	#region Helper Functions
	private void GenerateOptixObject()
	{
		try
		{
			string itemName = GetNavPath();
			if (itemName == "")
			{
				itemName = Title.TextId;
			}
			_navItem = InformationModel.Make<CoT_NavItem>(itemName);
			_isOpen = GetVariable("isOpen", _navItem);
			_visible = _navItem.VisibleVariable;
			_whiteBarVisible = GetVariable("whiteBarVisible", _navItem);
			_isWizard = GetVariable("isWizard", _navItem);
			GetVariable("title", _navItem).Value = Title;
			GetVariable("icon", _navItem).Value = Icon ?? "";
			GetVariable("testingID", _navItem).Value = Guid.NewGuid().ToString();
			_navItem.FindByType<Button>().OnMouseUp += OnClick;
			if (_navLogic.Columns.Count <= TreeNode.GetTreeLevel())
			{
				int diff = TreeNode.GetTreeLevel() + 1 - _navLogic.Columns.Count;
				for (int i = 0; i < diff; i++)
				{
					_navLogic.AddNavigationColumn(_navLogic.Columns.Count);
				}
			}
			_navLogic.Columns[TreeNode.GetTreeLevel()].Find("Layout").Add(_navItem);
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:GenerateOptixObject: {ex.Message}");
		}
	}

	/// <summary>
	/// Returns the navigation path of this element
	/// </summary>
	/// <returns>Naviagtion path formatted like "grandparent / parent / child"</returns>
	public string GetNavPath()
	{
		try
		{
			if (_navPath != string.Empty)
			{
				return _navPath;
			}

			if (TreeNode.GetTreeLevel() == 0)
			{
				_navPath = Title.Text;
				return _navPath;
			}
			else
			{
				_navPath = $"{TreeNode.GetParentValue().GetNavPath()} / {Title.Text}";
				return _navPath;
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:GetNavPath: {ex.Message}");
			return "";
		}
	}

	/// <summary>
	/// Method to open the navigation object
	/// All child objects are made visible in the navigation
	/// All sibling objects are closed
	/// If the objects was opened with a shortcut, the whole navigation path is opened
	/// </summary>
	public void Open()
	{
		try
		{
			bool wasOpen = IsOpen;
			if (TreeNode.GetTreeLevel() > 0)
			{
				TreeNode.GetParentValue().Open();

				foreach (NavItem sibling in TreeNode.GetSiblingsValues())
				{
					sibling.Close();
				}
			}
			else
			{
				_navLogic.CloseOtherRoots(TreeNode);
			}

			_isOpen.Value = true;
			_navLogic.SetColumnHeaderText(Title, TreeNode.GetTreeLevel() + 1);
			foreach (NavItem child in TreeNode.GetChildValues())
			{
				child.UpdateVisibility();
			}
			TreeNode.GetDescendantsValues()
			.Where(item => item.IsVisible && item.IsOpen).ToList()
			.ForEach(item => _navLogic.SetColumnHeaderText(item.Title, item.TreeNode.GetTreeLevel() + 1));
			if (!wasOpen)
			{
				_navLogic.HighlightScreenPath();
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:Open: {ex.Message}");
		}
	}

	/// <summary>
	/// Method that is called when an already open branch in the navigation is clicked
	/// It closes the branch and hides all child objects
	/// </summary>
	public void Close()
	{
		try
		{
			bool wasOpen = IsOpen;
			_isOpen.Value = false;
			TreeNode.GetChildValues().ForEach(child => child.UpdateVisibility());
			if (wasOpen)
			{
				_navLogic.HighlightScreenPath();
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:Close: {ex.Message}");
		}
	}

	/// <summary>
	/// Update the visibility for this item and all its descendants.
	/// </summary>
	public void UpdateVisibility()
	{
		try
		{
			_visible.Value = CheckVisibility();
			this.TreeNode.GetDescendantsValues().ForEach(node => node._visible.Value = node.CheckVisibility());
			_navLogic.UpdateColumnVisibility();
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:ChangeVisibility: {ex.Message}");
		}
	}

	private bool CheckVisibility()
	{
		try
		{
			if (TreeNode.GetTreeLevel() > 0 && (!TreeNode.GetParentValue()?._visible.Value || !TreeNode.GetParentValue()?._isOpen.Value))
			{
				return false;
			}
			if (_visibilityRestriction != null && !_visibilityRestriction?.Value)
			{
				return false;
			}
			if (TreeNode.Children.Count > 0)
			{
				return TreeNode.GetDescendantsValues().Count(item => item._visibilityRestriction != null ? item._visibilityRestriction.Value : true) > 0;
			}
			return true;
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:CheckVisibility: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Set or unset the path to the currently opened screen.
	/// </summary>
	/// <param name="isPath">True = set. False = unset.</param>
	public void Highlight(bool isPath)
	{
		_whiteBarVisible.Value = isPath;
	}

	/// <summary>
	/// Find a NavItem in the navigation structure that opens a certain screen
	/// </summary>
	/// <param name="screenId">NodeId of the screen</param>
	/// <returns></returns>
	public NavItem FindNavItem(NodeId screenId)
	{
		if (TreeNode.Children.Count > 0)
		{
			return TreeNode.GetDescendantsValues().Find(item => item.ScreenOpenId == screenId);
		}
		else
		{
			return (screenId == ScreenOpenId) ? this : null;
		}
	}
	#endregion

	#region Event Listeners
	private void ScreenVisibilityChanged(object sender, VariableChangeEventArgs e)
	{
		try
		{
			Log.Error($"Visibility changed to: {e.NewValue}");
			UpdateVisibility();
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:ScreenVisibilityChanged: {ex.Message}");
		}
	}

	/// <summary>
	/// Method to be called when the optix object is clicked
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void OnClick(object sender, MouseUpEvent e)
	{
		try
		{
			// Check if the object is a branch or a leaf in the navigation tree
			if (TreeNode.Children.Count > 0)
			{
				// Object is a branch
				if (IsOpen)
				{
					if (TreeNode.GetTreeLevel() == 0)
					{
						if (_navLogic.ScreenSize.Value == 2)
						{
							// Object is in the first level, is open and we are on a panel -> close the navigation
							_navLogic.DisplayNavigation(false);
						}
						else
						{
							// Object is in the first level, is open and we are not on a panel -> hide sidebar labels
							_navLogic.ChangeSideBarWidth(96);
						}
					}
					// Object was open already -> need to hide child objects in the navigation
					Close();
				}
				else
				{
					if (TreeNode.GetTreeLevel() == 0)
					{
						if (_navLogic.ScreenSize.Value == 2)
						{
							// Object is in the first level, is closed and we are on a panel -> open the navigation
							_navLogic.DisplayNavigation(true);
						}
					}
					// Object was closed before -> need to display child objects in the navigation
					Open();
				}
			}
			else
			{
				// Object is a leaf -> need to load a screen into the content panel
				// Actually open the screen in the content panel
				_navLogic.OpenScreenWithHistory(this);
			}
		}
		catch (Exception ex)
		{
			Log.Error($"Error in NavItem:OnClick: {ex.Message}");
		}
	}
	#endregion
}
