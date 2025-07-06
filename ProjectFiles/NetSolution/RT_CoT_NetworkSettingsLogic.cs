#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.Report;
using FTOptix.NetLogic;
using FTOptix.WebUI;
using FTOptix.Recipe;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Linq;
using FTOptix.TwinCAT;
using System.Collections.Generic;
using FTOptix.OPCUAServer;
using static RT_CoT_NetLogicAlarmsLogic;
using static RT_CoT_Helper;
#endregion


public class RT_CoT_NetworkSettingsLogic : BaseNetLogic
{   
    private LongRunningTask _fillTableTask;
    private IUANode _driversFolder;

    public override void Start()
    { 
        try
        {
            _driversFolder = GetPointer("commDriversFolder", LogicObject).GetPointedObj<IUANode>();
            _fillTableTask = new LongRunningTask(fillTable, LogicObject);
            _fillTableTask.Start();
        }
        catch (Exception ex)
        {            
            Logger.CreateAlarmAndLog($"Starting of _fillTableTask failed. '{ex.Message}");
        }        
    }

    private void fillTable(LongRunningTask task)
    {
        List<FTOptix.RAEtherNetIP.Station>RockwellStations = _driversFolder.FindNodesByType<FTOptix.RAEtherNetIP.Station>().ToList();
        bool hasRockwellStations = RockwellStations.Count > 0;
        GetVariable("stationsRockwell_Available", LogicObject).Value = hasRockwellStations; 

        List<FTOptix.TwinCAT.Station>BeckhoffStations = _driversFolder.FindNodesByType<FTOptix.TwinCAT.Station>().ToList();
        bool hasBeckhoffStations = BeckhoffStations.Count > 0;
        GetVariable("stationsBeckhoff_Available", LogicObject).Value = hasBeckhoffStations;

        int index = 0;
                                                                   
        ColumnLayout stationsRockwell_TableRows = GetPointer("stationsRockwell_TableRows_NodeId", LogicObject).GetPointedObj<ColumnLayout>();
        
        foreach (FTOptix.RAEtherNetIP.Station rockwellStation in RockwellStations)
        {
            IUAVariable routeVar = GetVariable("Route", rockwellStation);

            if (routeVar != null && routeVar.Value != null)
            {
                string route = routeVar.Value.Value.ToString().Trim();

                string[] parts = route.Split('\\');
                if (parts.Length >= 3)
                {
                    index++;

                    string station = rockwellStation.BrowseName;
                    string ipAddress = parts[0];
                    string slot = parts[2];                    
                    
                    TableRowStationRockwell newRow = InformationModel.MakeObject<TableRowStationRockwell>("row_" + index.ToString());
                    
                    GetVariable("station", newRow).Value = station;
                    GetVariable("ipAddress", newRow).Value = ipAddress;
                    GetVariable("slot", newRow).Value = slot;

                    stationsRockwell_TableRows.Add(newRow);
                }
                else
                {
                    Logger.CreateAlarmAndLog($"Route format unexpected for Station '{rockwellStation.BrowseName}': {route}");                    
                }
            }            
        }


        index = 0;
        
        ColumnLayout stationsBeckhoff_TableRows = GetPointer("stationsBeckhoff_TableRows_NodeId", LogicObject).GetPointedObj<ColumnLayout>();

        foreach (FTOptix.TwinCAT.Station beckhoffStation in BeckhoffStations)
        {
            index++;

            IUAVariable ipAddressVar = beckhoffStation.GetVariable("IpAddress");
            IUAVariable adsPortVar = beckhoffStation.GetVariable("AmsPort");
            IUAVariable remoteAmsNetIdVar = beckhoffStation.GetVariable("RemoteAmsNetId");
            IUAVariable localAmsNetIdVar = beckhoffStation.GetVariable("LocalAmsNetId");               

            string station = beckhoffStation.BrowseName;
            string ipAddress = ipAddressVar?.Value?.Value.ToString() ?? "";
            string adsPort = adsPortVar?.Value?.Value.ToString() ?? "";
            string remoteAmsNetId = remoteAmsNetIdVar?.Value?.Value.ToString() ?? "";
            string localAmsNetId = localAmsNetIdVar?.Value?.Value.ToString() ?? "";

            TableRowStationBeckhoff newRow = InformationModel.MakeObject<TableRowStationBeckhoff>("row_" + index.ToString());
                    
            GetVariable("station", newRow).Value = station;
            GetVariable("amsNetId", newRow).Value = remoteAmsNetId;  
            GetVariable("adsPort", newRow).Value = adsPort;                        

            stationsBeckhoff_TableRows.Add(newRow);         
        }  
    }
    

    public override void Stop()
    {
        _fillTableTask.Dispose();
    }
}
