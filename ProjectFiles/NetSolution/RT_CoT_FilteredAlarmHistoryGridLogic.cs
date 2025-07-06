#region Using directives
using System;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.UI;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.OPCUAServer;
using FTOptix.Store;
using FTOptix.SQLiteStore;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.NativeUI;
using FTOptix.TwinCAT;
#endregion

public class RT_CoT_FilteredAlarmHistoryGridLogic : BaseNetLogic
{
    IUAVariable toVariable;
    IUAVariable fromVariable;

    public override void Start()
    {
        // After checking validity, we set a default time interval of 24 hours
        toVariable = LogicObject.GetVariable("To");
        if (toVariable == null)
        {
            Log.Error("RT_CoT_FilteredAlarmHistoryGridLogic", "Missing To variable");
            return;
        }

        toVariable.Value = DateTime.Now;
        fromVariable = LogicObject.GetVariable("From");
        if (fromVariable == null)
        {
            Log.Error("RT_CoT_FilteredAlarmHistoryGridLogic", "Missing From variable");
            return;
        }

        fromVariable.Value = DateTime.Now.AddHours(-24);
    }

    [ExportMethod]
    public void Refresh()
    {
        if (toVariable != null)
        {
            toVariable.Value = DateTime.Now;
        }
    }


    [ExportMethod]
    public void SetPrintFilter()
    {
        MessageHistoryReportParam filterObject = InformationModel.Get(LogicObject.GetVariable("ReportFilter").Value) as MessageHistoryReportParam;

        if (filterObject == null)
        {
            Log.Error("RT_CoT_FilteredAlarmHistoryGridLogic", "Missing filter object");
            return;
        }

        if (fromVariable != null)
        {
            filterObject.FilterFrom = fromVariable.Value;
        }

        if (toVariable != null)
        {
            filterObject.FilterTo = toVariable.Value;
        }

        var criticalFilter = LogicObject.GetVariable("CriticalSelected");

        if (criticalFilter != null)
        {
            filterObject.CritcalSelected = criticalFilter.Value;
        }

        var errorFilter = LogicObject.GetVariable("ErrorSelected");

        if (errorFilter != null)
        {
            filterObject.ErrorSelected = errorFilter.Value;
        }

        var warninglFilter = LogicObject.GetVariable("WarningSelected");

        if (warninglFilter != null)
        {
            filterObject.WarningSelected = warninglFilter.Value;
        }

        var actionFilter = LogicObject.GetVariable("ActionSelected");

        if (actionFilter != null)
        {
            filterObject.ActionSelected = actionFilter.Value;
        }

        var infoFilter = LogicObject.GetVariable("InfoSelected");

        if (infoFilter != null)
        {
            filterObject.InfoSelected = infoFilter.Value;
        }
    }    
}
