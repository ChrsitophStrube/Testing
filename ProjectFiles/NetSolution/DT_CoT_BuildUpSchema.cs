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
using FTOptix.CoreBase;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.Alarm;
using FTOptix.Core;
using System.Collections.Generic;
using FTOptix.NativeUI;
using System.Reflection;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class DT_CoT_BuildUpSchema : BaseNetLogic
{
    NodePointer _tableRecipeParametersPointer =null;
    Folder _tableRecipeParametersFolder =null;

    NodePointer _pointerSchema;
    RecipeSchema _schema;

    ChildNodeCollection _schemaMetadataObjectsRoot;
    ChildNodeCollection _schemaMetadataObjectsEditModel;
    private void init()
    {
        //Get tableRecipeParameter Pointer
        _tableRecipeParametersPointer = LogicObject.Get<NodePointer>("RecipeParameters");
        //Get tableRecipeParameter Value
        _tableRecipeParametersFolder = InformationModel.Get<Folder>(_tableRecipeParametersPointer.Value);

        //Get Rechipe Schema Pointer
        _pointerSchema = LogicObject.Get<NodePointer>("RecipeSchema");
        //Get Rechipe Schema Value
        _schema = InformationModel.Get<RecipeSchema>(_pointerSchema.Value);

        _schemaMetadataObjectsRoot = _schema.Children.Get("Root").Children;
        _schemaMetadataObjectsEditModel = _schema.Children.Get("EditModel").Children;

    }
    [ExportMethod]
    public void addMetadataobjToRecipeSchema()
    {
        init();

        //clear EditModel and RecipeSchema
        _schemaMetadataObjectsRoot.Clear();
        _schemaMetadataObjectsEditModel.Clear();

        UAObject obj = (UAObject)InformationModel.MakeObject("Base");
        addMetadataobjToRecipeSchemaRecursively<CoT_CMP_RecipeParam>(_tableRecipeParametersFolder, obj);
        addMetadataobjToRecipeSchemaAndEditModel(obj);
        Log.Info("Add Format Store one more time");
    }
    private void addMetadataobjToRecipeSchemaRecursively<RecipeParamType>(UAObject tableRecipeParametersFolder, UAObject schemaObject)
    where RecipeParamType : IUAObject
    {
        string recipeParamTypeName = typeof(RecipeParamType).Name;
        foreach (var child in tableRecipeParametersFolder.Children)
        {
            if (child is UAObject uAObject)
            {
                if (recipeParamTypeName == uAObject.ObjectType.SuperType.BrowseName)
                {
                    //create Recipe Metadatatype
                    UAObject mO = (UAObject)InformationModel.MakeObject(child.BrowseName);

                    //create recipe value stored in the recipe
                    NodeId recipevalueDatatype = uAObject.FindVariable("recipeVariable")?.DataType;
                    mO.Add(InformationModel.MakeVariable("recipeVariable", recipevalueDatatype));
                    schemaObject.Add(mO);
                }
                else
                {
                    UAObject childObj = (UAObject)InformationModel.MakeObject(uAObject.BrowseName);
                    schemaObject.Add(childObj);
                    addMetadataobjToRecipeSchemaRecursively<RecipeParamType>(uAObject, childObj);
                }
            }
        }
    }


    private void addMetadataobjToRecipeSchemaAndEditModel(UANode schemaobj)
    {
        foreach (UAObject o in schemaobj.Children)
        {
            _schemaMetadataObjectsRoot.Add(o);
            _schemaMetadataObjectsEditModel.Add(o);
        }
    }

}
