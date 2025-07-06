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
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_CopyRecipeName : BaseNetLogic
{
    public override void Start()
    {
        createDefaltRecipename();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void createDefaltRecipename()
    {
        try
        {

            LogicObject.GetVariable("RecipeNameNew").Value = ((String)LogicObject.GetVariable("RecipeNameCopyFrom").Value) + "_Copy";
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CopyRecipeName", $"Error in createDefaltRecipename method: {ex.Message}");
        }
    }
}
