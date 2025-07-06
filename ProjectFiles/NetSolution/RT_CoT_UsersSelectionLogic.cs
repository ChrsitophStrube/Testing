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
using System.Linq.Expressions;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_UsersSelectionLogic : BaseNetLogic
{
	private IUANode _usersFolder;
	private IUANode _content;
	private IUAVariable _selectedUser;
	private IUAVariable _selectedOption;
	private Dictionary<int, CoT_User> _usersDict;
	public override void Start()
	{
		_usersFolder = InformationModel.Get(LogicObject.GetVariable("usersFolder").Value);
		_content = InformationModel.Get(LogicObject.GetVariable("content").Value);
		_selectedUser = LogicObject.GetVariable("selectedUser");
		_selectedOption = LogicObject.GetVariable("selectedOption");
		_selectedOption.VariableChange += OnSelectedOptionChanged;
		_usersDict = new Dictionary<int, CoT_User>();
		UpdateUsers();
	}

	public override void Stop()
	{
		_selectedOption.VariableChange -= OnSelectedOptionChanged;
	}

	private void OnSelectedOptionChanged(object sender, VariableChangeEventArgs e)
	{
		UpdateSelectedUser();
	}

	private void CreateUserEntry(CoT_User user)
	{
		var entry = InformationModel.Make<CoT_CTRL_RadioButton>(user.BrowseName);
		int index = 0;
		while (_usersDict.Any(i => i.Key == index))
		{
			index++;
		}
		_usersDict.Add(index, user);
		entry.GetVariable("optionID").Value = index;
		entry.GetVariable("selectedOption").SetDynamicLink(_selectedOption, DynamicLinkMode.ReadWrite);
		entry.GetVariable("text").Value = user.BrowseName;
		entry.GetVariable("icon").Value = user.icon;
		_content.Add(entry);
	}

	private List<CoT_User> GetUsers() => _usersFolder.GetNodesByType<CoT_User>().ToList();

	private void UpdateSelectedUser()
	{
		if (!_usersDict.ContainsKey(_selectedOption.Value))
		{
			Log.Error($"Selected user id doesn't exist.");
			return;
		}
		_selectedUser.Value = _usersDict[_selectedOption.Value].NodeId;
	}

	[ExportMethod]
	public void UpdateUsers()
	{
		var entriesBefore = _content.GetNodesByType<CoT_CTRL_RadioButton>().ToList();

		foreach (var user in GetUsers())
		{
			if (entriesBefore.Any(elem => elem.BrowseName == user.BrowseName))
			{
				entriesBefore.Remove(entriesBefore.Find(elem => elem.BrowseName == user.BrowseName));
			}
			else
			{
				CreateUserEntry(user);
			}
		}
		foreach (var entry in entriesBefore)
		{
			_usersDict.Remove(entry.GetVariable("optionID").Value);
			entry.Delete();
		}
		UpdateSelectedUser();
	}
}
