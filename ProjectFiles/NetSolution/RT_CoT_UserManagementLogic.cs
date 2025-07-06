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
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using CoT;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
#endregion

public class RT_CoT_UserManagementLogic : BaseNetLogic
{
	public static RT_CoT_UserManagementLogic ServerScript = null;

	private IUANode _usersFolder;
	private IUAVariable _usersChanged;
	private IUANode _dialogType;
	private IUANode _standardUser;
	private IUAVariable _standardUserPassword;

	public override void Start()
	{
		try
		{
			_usersFolder = InformationModel.Get(GetVariable("usersFolder", LogicObject).GetVariableValue<NodeId>());
			_usersChanged = GetVariable("usersChanged", LogicObject);
			_dialogType = InformationModel.Get(GetVariable("dialogType", LogicObject).GetVariableValue<NodeId>());
			_standardUser = GetPointer("standardUser", LogicObject).GetPointedObj<CoT_User>();
			_standardUserPassword = GetVariable("standardUserPassword", LogicObject);

			if (ServerScript == null)
			{
				ServerScript = this;
			}
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("Alarm Init Fault", MessageType.Warning, ex, LogicObject);
		}
	}

	public override void Stop()
	{
		if (ServerScript == this)
		{
			ServerScript = null;
		}
	}

	[ExportMethod]
	public void CreateUser(string username, string displayname, string password, NodeId sessionId)
	{
		foreach (var existingUser in _usersFolder.GetNodesByType<CoT_User>())
		{
			if (existingUser.BrowseName.Equals(username, StringComparison.OrdinalIgnoreCase))
			{

				Logger.CreateAlarmAndLog($"Trying to create user {username} failed. User already exists.");
				return;
			}
		}

		var user = InformationModel.Make<CoT_User>(username);
		_usersFolder.Add(user);

		var result = InformationModel.Get<Session>(sessionId).ChangePassword(username, password, string.Empty);

		switch (result.ResultCode)
		{
			case FTOptix.Core.ChangePasswordResultCode.Success:
				break;
			case FTOptix.Core.ChangePasswordResultCode.WrongOldPassword:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.PasswordAlreadyUsed:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.PasswordChangedTooRecently:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.PasswordTooShort:
				_usersFolder.Remove(user);
				Logger.CreateAlarmAndLog($"Trying to create user {username} failed. Password too short.");
				return;
			case FTOptix.Core.ChangePasswordResultCode.UserNotFound:
				//Not applicable
				break;
			case FTOptix.Core.ChangePasswordResultCode.UnsupportedOperation:
				_usersFolder.Remove(user);
				Logger.CreateAlarmAndLog($"Trying to create user {username} failed. Unsupported operation.");
				return;
		}

		user.userName = displayname;
	}

	[ExportMethod]
	public void Login(NodeId sessionId, string userName, string password)
	{
		var session = InformationModel.Get<Session>(sessionId);
		var result = session.ChangeUser(userName, password);
		if (result.ResultCode != ChangeUserResultCode.Success)
		{
			String dialogText = String.Empty;
			switch (result.ResultCode)
			{
				case ChangeUserResultCode.UserNotFound:
					dialogText = $"User {userName} doesn't exist.";
					break;
				case ChangeUserResultCode.WrongPassword:
					dialogText = $"Wrong password.";
					break;
				case ChangeUserResultCode.HomonymUsers:
					dialogText = $"Multiple users with name {userName} exist.";
					break;
				default:
					dialogText = "Login failed due to unknown reason";
					break;
			}
			ShowDialog(sessionId, dialogText, "Login failed");
		}
	}

	[ExportMethod]
	public void Logout(NodeId sessionId)
	{
		var session = InformationModel.Get<Session>(sessionId);
		var result = session.ChangeUser(_standardUser.BrowseName, _standardUserPassword.Value);
		if (result.ResultCode != ChangeUserResultCode.Success)
		{
			ShowDialog(sessionId, "Logout failed. Standard user login failed.", "Logout failed");
		}
	}

	[ExportMethod]
	public void UsersChanged()
	{
		_usersChanged.Value = !_usersChanged.Value;
	}

	public CoT_User GetUser(string name)
	{
		return _usersFolder.Find<CoT_User>(name);
	}

	[ExportMethod]
	public void ChangePassword(NodeId sessionId, string username, string oldPassword, string newPassword, string confirmPassword)
	{
		UISession _uiSession = (UISession)InformationModel.Get(sessionId);
		bool success = true;
		String dialogText = String.Empty;

		if (newPassword != confirmPassword)
		{
			success = false;
			dialogText = "New Password doesn't match password confirmation";
		}
		else
		{
			ChangePasswordResult result = _uiSession.ChangePassword(username, newPassword, oldPassword);
			if (!result.Success)
			{
				success = false;
				switch (result.ResultCode)
				{
					case ChangePasswordResultCode.PasswordTooShort:
						dialogText = "The given password was too short";
						break;
					case ChangePasswordResultCode.UserNotFound:
						dialogText = $"User {username} doesn't exist";
						break;
					case ChangePasswordResultCode.WrongOldPassword:
						dialogText = "The previous password was incorrect";
						break;
					default:
						dialogText = "Changing the password failed due to unknown reason";
						break;
				}
			}
		}
		if (!success)
		{
			ShowDialog(sessionId, dialogText, "Password change failed");
		}
		else
		{
			if (username == _standardUser.BrowseName)
			{
				_standardUserPassword.Value = newPassword;
			}
			ShowDialog(sessionId, $"Password for {username} was changed successfully", "Password changed");
		}
	}

	private void ShowDialog(NodeId sessionId, string dialogText, string dialogTitle)
	{
		UISession _uiSession = (UISession)InformationModel.Get(sessionId);
		var _mainWindow = _uiSession.Get("UIRoot");
		String _locale = _uiSession.User.LocaleId;

		var alias = InformationModel.MakeObject<CoT_DialogAlias>("dialogAlias");
		alias.dialogText = new LocalizedText(dialogText, _locale);
		alias.headerText = new LocalizedText(dialogTitle, _locale);
		UICommands.OpenDialog(_mainWindow, (DialogType)_dialogType, alias.NodeId);
	}
}
