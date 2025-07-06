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
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Reflection.Metadata.Ecma335;
using FTOptix.NativeUI;
using System.Linq;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_TableRowCreater : BaseNetLogic
{
    NodePointer _recipeParamsFoldersPointer = null;
    Folder _recipeParamsFoldersFolder = null;
    NodePointer _rowPanelPointer = null;
    Panel _rowPanel = null;
    NodePointer _tableContentpanelLoaderPointer = null;
    ColumnLayout _tableContentpanelLoader = null;

    private LongRunningTask _createTableTask;



    public override void Start()
    {
        try
        {
            _createTableTask = new LongRunningTask(createTable, LogicObject);
            _createTableTask.Start();
        }
        catch (Exception ex)
        {
            Log.Error("Table Rows creation init Fault");
        }

        }

    public override void Stop()
    {
        _createTableTask.Dispose();
    }

    private void createTable(LongRunningTask task)
    {
        init();
    }

    private void init()
    {
        //Get tableRecipeParameter Pointer
        _recipeParamsFoldersPointer = LogicObject.Get<NodePointer>("recipeParamsFolder");

        //Get tableRecipeParameter Value
        _recipeParamsFoldersFolder = InformationModel.Get<Folder>(_recipeParamsFoldersPointer.Value);

        //Get rowPanel Pointer
        _rowPanelPointer = LogicObject.Get<NodePointer>("rowPanel");

        //Get rowPanel
        _rowPanel = InformationModel.Get<Panel>(_rowPanelPointer.Value);

        //Get tableContentpanelLoader Pointer
        _tableContentpanelLoaderPointer = LogicObject.Get<NodePointer>("tableContentpanelLoader");

        //Get tableContentpanelLoader
        _tableContentpanelLoader = InformationModel.Get<ColumnLayout>(_tableContentpanelLoaderPointer.Value);
    }

    List<CoT_CMP_RecipeParam> _metadataObjectsLastFilterRun = new();
    [ExportMethod]
    public void createTableContentFiltered(string paramValue1, string paramNameFilter1, string paramValue2, string paramNameFilter2, string paramValue3, string paramNameFilter3)
    {
        try
        {
            List<CoT_CMP_RecipeParam> metadataObjects = RT_CoT_RecipeHelper.getMetadataObjects<CoT_CMP_RecipeParam>(_recipeParamsFoldersFolder);
            if (paramValue1 != "" && paramValue1 != "No Selection")
            {
                metadataObjects = filterMetadata(metadataObjects, paramValue1, paramNameFilter1);
            }
            if (paramValue2 != "" && paramValue2 != "No Selection")
            {
                metadataObjects = filterMetadata(metadataObjects, paramValue2, paramNameFilter2);
            }
            if (paramValue3 != "" && paramValue3 != "No Selection")
            {
                metadataObjects = filterMetadata(metadataObjects, paramValue3, paramNameFilter3);
            }
            //skip objects if twice existing
            if (_metadataObjectsLastFilterRun.SequenceEqual(metadataObjects))
            {
                return;
            }

            //shallow copy liste
            _metadataObjectsLastFilterRun = new(metadataObjects);
            _tableContentpanelLoader.Children.Clear();
            createTableContent(metadataObjects);
        }
        catch (Exception ex)
        {
            Log.Error("Table row creation Filter faulted");
        }
    }

    private void createTableContent<RecipeParamType>(List<RecipeParamType> metadataObjects)
    where RecipeParamType : IUAObject
    {
        foreach (RecipeParamType metadataObject in metadataObjects)
        {
            TableRowRecipeFormat _rowFormat = InformationModel.MakeObject<TableRowRecipeFormat>("row_" + metadataObject.BrowseName);
            IUAVariable _metadataPointer = _rowFormat.GetVariable("metadata");
            _metadataPointer.Value = metadataObject.NodeId;
            _tableContentpanelLoader.Add(_rowFormat);
        }
    }

    private List<RecipeParamType> filterMetadata<RecipeParamType>(List<RecipeParamType> metadataObjects, string paramValue, string paramNameFilter)
    where RecipeParamType : IUAObject
    {
        return metadataObjects.FindAll(recipeParamoj => ((IUAVariable)recipeParamoj.Get(paramNameFilter)).Value == paramValue);
    }

}

public static class RT_CoT_RecipeHelper
{

    public static List<RecipeParamType> getMetadataObjects<RecipeParamType>(UAObject tableRecipeParametersFolder)
    where RecipeParamType : IUAObject
    {
        List<RecipeParamType> metadataObjects = new List<RecipeParamType>();

        foreach (var child in tableRecipeParametersFolder.Children)
        {
            if (child is RecipeParamType param)
            {
                metadataObjects.Add(param);
            }
            else if (child is UAObject uaObj)
            {
                metadataObjects.AddRange(getMetadataObjects<RecipeParamType>(uaObj));
            }
        }
        return metadataObjects;
    }

}
