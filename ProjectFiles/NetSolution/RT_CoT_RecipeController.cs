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
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.Core;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Numerics;
using System.Collections;
using FTOptix.NativeUI;
using System.ComponentModel.Design.Serialization;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using static RT_CoT_Helper;
using static RT_CoT_NetLogicAlarmsLogic;
using CoT;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_RecipeController : BaseNetLogic
{
    NodePointer _pointerSchema;
    RecipeSchema _schema;

    public override void Start()
    {
        try
        {
            initVars();
        }
        catch (Exception ex)
        {
            Logger.CreateAlarmAndLog("Fault Init Recipe Schema)", MessageType.Warning, ex, LogicObject);
        }
    }
    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }
    private void initVars()
    {

        _schema = GetPointer("RecipeSchema", LogicObject).GetPointedObj<RecipeSchema>();
    }

    [ExportMethod]
    public void CreateRecipeFromDefaultValues(String name, NodeId resultVar)
    {
        try
        {
            // Set Led State to Loading
            UAVariable _newCreationResultState = (UAVariable)InformationModel.Get(resultVar);
            _newCreationResultState.RemoteWrite(8);

            List<string> browseNames = GetSchemaBrowsePaths("", (UANode)_schema.Get("Root"));

            NodeId targetFolderNodeId = _schema.TargetNode;
            Folder targetFolder = (Folder)InformationModel.Get(targetFolderNodeId);

            Dictionary<string, UAVariable> schemaDefaultValuesDict = GetRecipeDefaultValues(browseNames, targetFolder);
            NodeId _editModelID = FillEditmodelWithMetadata(schemaDefaultValuesDict);
            CreateRecipe(_editModelID, name);

            //Set Result to Finished
            _newCreationResultState.Value = 6;

        }
        catch (Exception ex)
        {
            // Set result to Error
            UAVariable _newCreationResultState = (UAVariable)InformationModel.Get(resultVar);
            _newCreationResultState.Value = 3;
            Logger.CreateAlarmAndLog("CouldNot Create Recipe from Default Schema", MessageType.Warning, ex, LogicObject);
        }
    }

    private List<string> GetSchemaBrowsePaths(string path, UANode uAObject)
    {
        List<string> paths = new();
        foreach (UANode child in uAObject.Children)
        {
            string newPath = string.IsNullOrEmpty(path)
                ? child.BrowseName
                : $"{path}/{child.BrowseName}";

            if (child is UAVariable)
            {
                paths.Add(newPath);
                return paths;
            }

            if (child is UAObject)
            {
                paths.AddRange(GetSchemaBrowsePaths(newPath, child));
            }
        }

        return paths;
    }

    private Dictionary<string, UAVariable> GetRecipeDefaultValues(List<string> _schemaBrowsePaths, Folder _schemaFolder)
    {
        var _schemaDefaultValuesDict = new Dictionary<string, UAVariable>();
        foreach (string schemaBrowsePath in _schemaBrowsePaths)
        {var metadata = _schemaFolder.Get(schemaBrowsePath);

            UAVariable _defaultValue = (UAVariable)metadata.Owner.Get("defaultValue");
            _schemaDefaultValuesDict.Add(schemaBrowsePath, _defaultValue);
        }
        return _schemaDefaultValuesDict;
    }

    private NodeId FillEditmodelWithMetadata(Dictionary<string, UAVariable> _schemaDefaultValuesDict)
    {
        UAObject _editModel = _schema.Get<UAObject>("EditModel");

        foreach (KeyValuePair<string, UAVariable> kvp in _schemaDefaultValuesDict)
        {
            UAVariable editmodelValue = (UAVariable)_editModel.GetVariable(kvp.Key);
            editmodelValue.Value = kvp.Value.Value;
        }
        return _editModel.NodeId;

    }

    private void CreateRecipe(NodeId _editModelId, string _recipeName)
    {
        // Create recipe
        _schema.CreateStoreRecipe(_recipeName);
        // Save Recipe
        _schema.CopyToStoreRecipe(_editModelId, _recipeName, CopyErrorPolicy.BestEffortCopy);
    }
}
