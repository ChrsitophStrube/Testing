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
using System.Collections.Generic;
using System.Linq;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_UserGroupsSelectionLogic : BaseNetLogic
{
	private IUAVariable _selectedUserId;
	private IUANode _selectedUser;
	private List<Group> _groups;
	private IUANode _content;
	private Dictionary<Group, IUANode> _checkboxDict;
	
	public override void Start()
	{
		_selectedUserId = LogicObject.GetVariable("selectedUser");
		_selectedUserId.VariableChange += OnSelectedUserChanged;
		_selectedUser = InformationModel.Get(_selectedUserId.Value);
		_groups = InformationModel.Get(LogicObject.GetVariable("groupsFolder").Value).GetNodesByType<Group>().ToList();
		_content = InformationModel.Get(LogicObject.GetVariable("content").Value);
		_checkboxDict = new Dictionary<Group, IUANode>();
		foreach(var group in _groups)
		{
			var checkbox = InformationModel.Make<CoT_CTRL_CheckBox>(group.BrowseName);
			checkbox.GetVariable("text").Value = group.BrowseName;
			_checkboxDict.Add(group, checkbox);
			_content.Add(checkbox);
		}
		UpdateGroupsSelection();
	}

	public override void Stop()
	{
		if(_selectedUserId != null)
		{
			_selectedUserId.VariableChange -= OnSelectedUserChanged;
		}
	}
	
	private void UpdateGroupsSelection()
	{
		foreach(var group in _groups)
		{
			var check = _checkboxDict[group].GetVariable("checked");
			if(UserHasGroup(group.NodeId, _selectedUser))
			{ 
				check.Value = true; 
			} 
			else 
			{ 
				check.Value = false; 
			}
		}
	}
	
	private bool UserHasGroup(NodeId group, IUANode user)
	{
		if(user == null)
		{
			return false;
		}
		else
		{
			var userGroups = user.Refs.GetObjects(FTOptix.Core.ReferenceTypes.HasGroup);
			return userGroups.Any(elem => elem.NodeId == group);
		}
	}

	private void OnSelectedUserChanged(object sender, VariableChangeEventArgs e)
	{
		_selectedUser = InformationModel.Get(_selectedUserId.Value);
		UpdateGroupsSelection();
	}
	
	[ExportMethod]
	public void ApplyGroups()
	{
		try
		{
			foreach(var group in _groups)
			{
				if(_checkboxDict[group].GetVariable("checked").Value)
				{
					_selectedUser.Refs.AddReference(FTOptix.Core.ReferenceTypes.HasGroup, group);
				}
				else
				{
					_selectedUser.Refs.RemoveReference(FTOptix.Core.ReferenceTypes.HasGroup, group.NodeId, false);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Error(LogicObject.BrowseName, $"Error in ApplyGroups: {ex.Message}");
		}
	}
	
	[ExportMethod]
	public void CreateUserWithGroups(string username, string displayname, string password, NodeId sessionId, NodeId userManagementScript)
	{
		try
		{
			RT_CoT_UserManagementLogic.ServerScript.CreateUser(username, displayname, password, sessionId);

			_selectedUser = RT_CoT_UserManagementLogic.ServerScript.GetUser(username);
			
			ApplyGroups();
		}
		catch (Exception ex)
		{
			Log.Error(LogicObject.BrowseName, $"Error in CreateUserWithGroups: {ex.Message}");
		}
	}
}
