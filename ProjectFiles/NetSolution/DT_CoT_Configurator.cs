#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.WebUI;
using FTOptix.System;
using FTOptix.NetLogic;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.Alarm;
using FTOptix.CommunicationDriver;
using FTOptix.Core;
using System.Collections.Generic;
using FTOptix.TwinCAT;
using FTOptix.NativeUI;
using FTOptix.EventLogger;
using FTOptix.Report;
using FTOptix.DataLogger;
using FTOptix.OPCUAServer;
#endregion

public class DT_CoT_Configurator : BaseNetLogic
{
    private ResourceUri _xmlPathPhi;

    private TreeNode<CtOUINode> _screenConfiguration;
    private TreeNode<CtOUINode> _alarmConfiguration;
    private TreeNode<CtOUINode> _cfgConfiguration;

    private OptixObjectCreator _myOptixObjectCreator;



    [ExportMethod]
    public void RunConfigurator()
    {



        _xmlPathPhi = new ResourceUri(LogicObject.GetVariable("phiConfiguratorInput").Value);
        _myOptixObjectCreator = new OptixObjectCreator();
        var xmlParser = new CtoXmlParser(_xmlPathPhi.Uri);
        _screenConfiguration = xmlParser.CreateScreenModel();
        _alarmConfiguration = xmlParser.CreateAlarmModel();
        _cfgConfiguration = xmlParser.CreateCfgModel();
        var alarmCreatorTask = new LongRunningTask(StartAlarmsAsync, LogicObject);
        alarmCreatorTask.Start();
        var cfgCreatorTask = new LongRunningTask(StartCfgsAsync, LogicObject);
        cfgCreatorTask.Start();
        var screenCreatorTask = new LongRunningTask(CreateScreenAsync, LogicObject);
        screenCreatorTask.Start();
        
    }

    private void CreateScreenAsync()
    {
        List<CtOUINode> preOrderTraversal = _screenConfiguration.GetPreOrderValues();
        ScreenCreator screenCreator = new();
        var nodeList = screenCreator.genereatePLCLinks(preOrderTraversal);
        // Create all screens
        foreach (var screen in nodeList)
        {
            if (screen.IsNewOptixType)
            {
                _myOptixObjectCreator.CreateOptixUIObject(screen, screen.IsNewOptixType);
            }
        }
        // Create all elements on screens
        foreach (var node in nodeList)
        {

            if (node.OptixType != null && !node.IsNewOptixType)
            {

                _myOptixObjectCreator.CreateOptixUIObject(node, node.IsNewOptixType);
            }

        }
        // For complete faceplate screens add the links to the the main object again
        foreach (var screenNode in nodeList)
        {
            if (screenNode.ScreenLinks.Keys.Count > 0)
            {
                _myOptixObjectCreator.UpdateScreenLinks(screenNode);
            }
        }


    }
    private void StartCfgsAsync()
    {
        CfgCreator.CreateCfgs(_cfgConfiguration, _myOptixObjectCreator);
        CfgCreator.CreateFacePlateCfgs(_cfgConfiguration, _myOptixObjectCreator);

    }
    private void StartAlarmsAsync()
    {

        AlarmCreator.CreateAlarms(_alarmConfiguration, _myOptixObjectCreator);
        AlarmCreator.CreateFacePlateAlarms(_alarmConfiguration, _myOptixObjectCreator);

    }

}


