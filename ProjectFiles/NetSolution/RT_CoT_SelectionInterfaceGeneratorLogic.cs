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
using System.IO;
using FTOptix.NativeUI;
using System.Numerics;
using System.Threading.Tasks;
using System.Reflection;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_SelectionInterfaceGeneratorLogic : BaseNetLogic
{
	LongRunningTask asyncTask;
	IUANode _selectionInterface;
	IUAVariable _optionsStringVar;
	IUAVariable _querryVar;
	NetLogicObject _selectionInterfaceNetlogic;
	Store _db;
	public override void Start()
	{
		try
		{
			_selectionInterfaceNetlogic = (NetLogicObject)InformationModel.GetObject(GetVariable("selectionInterfaceNetLogic", LogicObject).GetVariableValue<NodeId>());
			_selectionInterface = InformationModel.Get(LogicObject.GetVariable("selectionInterface").Value);
			_optionsStringVar = LogicObject.GetVariable("optionsString");

			if (_optionsStringVar != null)
			{
				_optionsStringVar.VariableChange += createOptionsData;
			}

			_db = InformationModel.Get(GetVariable("dataBase", LogicObject).GetVariableValueOrDefault<NodeId>()) as Store;
			_querryVar = GetVariable("query", LogicObject);

			if (_querryVar != null)
			{
				_querryVar.VariableChange += createOptionsData;
			}

			createOptionsData();
		}
		catch (Exception ex)
		{
			Logger.CreateAlarmAndLog("Dropdown: OptionData Init Fault", MessageType.Warning, ex, LogicObject);
		}

	}

	private void createOptionsData(object sender, VariableChangeEventArgs e)
	{
		Initialize();
		_selectionInterfaceNetlogic.ExecuteMethod("ResetSelectOptionToFirstElement");

	}

	[ExportMethod]
	public void createOptionsData()
	{

		LongRunningTask asyncTask = new LongRunningTask(Initialize, LogicObject);
		asyncTask.Start();
	}

	private void Initialize()
	{
		//Create from String
		string optionsString = _optionsStringVar?.Value;
		List<OptionValue> options = new List<OptionValue>();
		if (optionsString != "")
		{
			options = GenerateOptionsDataFromString(optionsString);
		}

		//Create from DB
		string querry = _querryVar?.Value;
		querry = querry.Trim();
		if (_db != null && querry != String.Empty)
		{
			options = GenerateOptionsDataFromSQL(_db, querry);

		}

		//create fom Optix Objects
		var folderId = LogicObject.GetVariable("objectsFolder")?.Value;
		Folder objectsFolder = null;
		if (folderId != null)
		{
			objectsFolder = (Folder)InformationModel.Get(folderId);

			string nameObjectId = GetVariable("nameObjectId", LogicObject).GetVariableValueOrDefault<string>();
			string nameObjectTextKey = GetVariable("nameObjectTextKey", LogicObject).GetVariableValueOrDefault<string>();
			string nameObjectIcon = GetVariable("nameObjectIcon", LogicObject).GetVariableValueOrDefault<string>();
			if (nameObjectTextKey != String.Empty)
			{
				options = GenerateOptionsDataFromOptixObjects(objectsFolder, nameObjectId, nameObjectTextKey, nameObjectIcon);
			}
		}


		GenerateInterfaceOptions(options);
	}

	public override void Stop()
	{
		if (asyncTask != null)
		{
			asyncTask?.Dispose();
		}
	}


	private void ClearOptions()
	{
		List<CoT_SelectionOption> options = _selectionInterface.Children.OfType<CoT_SelectionOption>().ToList();
		foreach (var option in options)
		{
			option.Delete();
		}
	}

	private List<OptionValue> GenerateOptionsDataFromString(string optionsString)
	{
		List<OptionValue> optionValues = new List<OptionValue>();

		string[] optionsArray = optionsString.Trim().Split(";");

		for (int i = 0; i < optionsArray.Length; i++)
		{
			string[] option = optionsArray[i].Trim().Split(",");

			if (option.Length == 0 || option.Length > 3)
			{
				Logger.CreateAlarmAndLog("Options String wrong formatted");
				return optionValues;
			}
			if (option.Length == 1 && option[0] == "")
			{
				continue;
			}

			OptionValue o = null;

			if (option.Length == 1)
			{
				o = new OptionValue(i + 1, option[0]);
			}

			if (option.Length == 2)
			{
				if (int.TryParse(option[0], out int id))
				{
					o = new OptionValue(id, option[1]);
				}
				else
				{
					o = new OptionValue(i + 1, option[0], option[1]);
				}
			}

			if (option.Length == 3)
			{
				if (int.TryParse(option[0], out int id))
				{
					o = new OptionValue(id, option[1], option[2]);
				}
				else
				{
					Logger.CreateAlarmAndLog("Options String wrong formatted");
				}
			}
			optionValues.Add(o);
		}
		return optionValues;
	}

	private List<OptionValue> GenerateOptionsDataFromSQL(Store db, string query)
	{
		List<OptionValue> optionValues = new List<OptionValue>();

		Object[,] resultSet;
		String[] header;
		db.Query(query, out header, out resultSet);

		int idIndex = Array.FindIndex(header, a => a == "ID");
		int textIndex = Array.FindIndex(header, a => a == "Text");
		int iconIndex = Array.FindIndex(header, a => a == "Icon");

		if (textIndex == -1)
		{
			Logger.CreateAlarmAndLog("Text not found in database");
			return optionValues;
		}

		for (int i = 0; i < resultSet.GetLength(0); i++)
		{
			OptionValue o;
			int id;

			if (idIndex == -1)
			{
				id = i + 1;
			}
			else
			{
				if (!int.TryParse(resultSet[i, idIndex].ToString(), out id))
				{
					Logger.CreateAlarmAndLog("id not found in database");
				}

			}

			if (iconIndex == -1)
			{
				o = new OptionValue(id, resultSet[i, textIndex].ToString());
			}
			else
			{
				o = new OptionValue(id, resultSet[i, textIndex].ToString(), resultSet[i, iconIndex].ToString());
			}
			optionValues.Add(o);
		}
		return optionValues;
	}

	private List<OptionValue> GenerateOptionsDataFromOptixObjects(Folder objectsFolder, string nameObjectId, string nameObjectTextKey, string nameObjectIcon)
	{
		List<OptionValue> optionValues = new List<OptionValue>();

		for (int i = 0; i < objectsFolder.Children.Count; i++)
		{
			var id = ((IUAVariable)objectsFolder.Children[i].Get(nameObjectId))?.Value;
			if (id == null)
			{
				id = i + 1;
			}
			var textKey = ((IUAVariable)objectsFolder.Children[i].Get(nameObjectTextKey))?.Value;
			if (textKey == "")
			{
				Logger.CreateAlarmAndLog("Element text key empty ");
			}
			var icon = ((IUAVariable)objectsFolder.Children[i].Get(nameObjectIcon))?.Value;


			if (icon == null)
			{
				optionValues.Add(new OptionValue(id, textKey));
			}
			else
			{
				optionValues.Add(new OptionValue(id, textKey, icon));
			}
		}

		return optionValues;
	}



	private void GenerateInterfaceOptions(List<OptionValue> optionValues)
	{
		if (optionValues.Count == 0)
		{
			Logger.CreateAlarmAndLog("Dropdown is empty.", MessageType.Info);
			return;
		}

		ClearOptions();
		foreach (OptionValue option in optionValues)
		{
			//MakeOption object
			CoT_SelectionOption o = InformationModel.Make<CoT_SelectionOption>($"Option{option.Id}");

			// assign ID
			o.id = option.Id;

			// assign Localized text
			o.text = option.LocText;

			// assign text key
			o.textKey = option.TextKey;

			// assign Icon
			if (option.Icon != String.Empty)
			{
				o.icon = ResourceUri.FromProjectRelativePath($"Icons/{option.Icon}");
			}

			_selectionInterface.Add(o);
			if (option.Equals(optionValues.First()))
			{
				_selectionInterface.GetVariable("selectedOption").Value = o.NodeId;
			}
		}
	}
}

public class OptionValue
{
	public int Id { get; private set; }
	public LocalizedText LocText { get; private set; }

	public string TextKey { get; private set; }

	public string Icon { get; private set; }

	public OptionValue(int id, string textKey) : this(id, textKey, string.Empty) // Constructore-Cascade
	{
	}

	public OptionValue(int id, string textKey, string icon)
	{
		//set ID
		Id = id;

		TextKey = textKey;

		//lookup LocalizedText for key
		LocalizedText lockey = new LocalizedText(Project.Current.NodeId.NamespaceIndex, textKey);
		LocText = InformationModel.LookupTranslation(lockey);

		// check if translation Exists
		if (!LocText.HasTranslation)
		{
			LocText = new LocalizedText(-1, textKey, textKey, "");
		}

		//set Icon
		Icon = icon;
	}
}


