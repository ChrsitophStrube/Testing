#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Text;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_FilterCreator : BaseNetLogic
{
    Folder _objectsFolder = null;
    public override void Start()
    {
        try
        {


            var folderId = LogicObject.GetVariable("recipeParamsFolder")?.Value;

            if (folderId != null)
            {
                _objectsFolder = (Folder)InformationModel.Get(folderId);
            }
            string groupDropdownOptions = "";
            groupDropdownOptions = createGroupsFilterString("paramGroup", "", "");
            //cut no selection for first combobox
            groupDropdownOptions = groupDropdownOptions.Replace("No Selection;", "");

            IUAVariable netlogicgroupDropdownOptions = LogicObject.GetVariable("groupsFilterString");

            netlogicgroupDropdownOptions.Value = groupDropdownOptions;
        }
        catch (Exception ex)
        {
            Log.Error("Filter creator init Exception");
        }

    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    [ExportMethod]
    public void createGroupsFilterString(string paramName, string paramNameFilter, string filter, NodeId filterStringNodeID)
    {
        try{
        string filterString = "";
        filterString = createGroupsFilterString(paramName, paramNameFilter, filter);
        IUAVariable filteroutVariable = (IUAVariable)InformationModel.Get(filterStringNodeID);
        filteroutVariable.Value = filterString;
        }
        catch (Exception ex)
        {
            Log.Error("Filter creator Exception");
        }
    }


    private string createGroupsFilterString(string paramName, string paramNameFilter, string filter)
    {
        var sb = new StringBuilder();
        sb.Append("No Selection;");

        if (filter.Equals("No Selection"))
        {
            return "No Selection";

        }

        List<string> propertiesList = buildPropertiesList<CoT_CMP_RecipeParam>(new List<string>(),_objectsFolder, paramName, paramNameFilter, filter);

        foreach (string property in propertiesList)
        {
            sb.Append(property + ";");
        }
        //cut last ;
        sb.Remove(sb.Length - 1, 1);
        return sb.ToString();
    }


    private List<string> buildPropertiesList<RecipeParamType>(List<string> propertiesList, UAObject tableRecipeParametersFolder, string paramName, string paramNameFilter, string filter)
    where RecipeParamType : IUAObject
    {
        List<RecipeParamType> metadataObjects = RT_CoT_RecipeHelper.getMetadataObjects<RecipeParamType>(tableRecipeParametersFolder);
        foreach (RecipeParamType metadataObject in metadataObjects)
        {
            var textKey = ((IUAVariable)metadataObject.Get(paramName))?.Value;
            var filterValue = ((IUAVariable)metadataObject.Get(paramNameFilter))?.Value;

            if (
            !propertiesList.Contains((String)textKey)
            &&
            (filterValue?.Equals(filter) ?? true)
            &&
            textKey!=""
            )
            {
                propertiesList.Add((String)textKey);
            }
        }
        return propertiesList;
    }
}
