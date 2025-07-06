using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using FTOptix.Alarm;
using FTOptix.AuditSigning;
using FTOptix.HMIProject;
using FTOptix.UI;
using UAManagedCore.OpcUa;
using UAManagedCore;

/** 
This class should work as a wrapper for MakeObject from the optix libiray, that handeles our custom properties in a good way.
This will be our interface class to create optix objects during the design time and should work as our interface to create everything given a defined structure from the outside



-- Input
 Optix object

 -- Output 
     SUccessfull bool if the object is created or a fault code if not.

**/


class OptixObjectCreator
{
    // with reference to optix project    



    public OptixObjectCreator()
    {
    }

    public void UpdateScreenLinks(CtOUINode uiNode)
    {
        var currentNode = Project.Current.Find(uiNode.Name);
        foreach (var screenLinks in uiNode.ScreenLinks)
        {
            AddFixedProperty(currentNode, screenLinks.Key, screenLinks.Value);
        }


    }
    public int CreateOptixUIObject(CtOUINode uiNode, bool isType)
    {
        try
        {
            var myCustomType = Project.Current.Find(uiNode.OptixType);

            if ((myCustomType != null) && (uiNode.Name != null))
            {
                IUANode myCustomObject;
                if (isType)
                {
                    myCustomObject = InformationModel.MakeObjectType(uiNode.Name, myCustomType.NodeId);
                }
                else
                {
                    myCustomObject = InformationModel.MakeObject(uiNode.Name, myCustomType.NodeId);
                }

                if (myCustomObject != null)
                {
                    AddProperties(myCustomObject, uiNode);

                    var myProject = CreatePathWhenNotExist(uiNode.PathToNode);
                    if (myProject != null)
                    {
                        DeleteObjectIfExists(myProject, uiNode.Name);

                        myProject.Add(myCustomObject);
                    }
                    else
                    {
                        Log.Error("Could not find path: " + uiNode.PathToNode);
                    }
                }
                else
                {
                    Log.Error("Could not create object: " + uiNode.Name);
                }
            }
            else
            {
                if (myCustomType == null)
                {
                    Log.Error(uiNode.Name + " Missing object type: " + uiNode.OptixType);
                }

                if (uiNode.Name == null)
                {
                    Log.Error(uiNode.Name + " Missing object name: " + uiNode.Name);
                }
            }

            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("Exception during object creating: " + ex.Message);

            return -1;
        }
    }

    private IUANode CreatePathWhenNotExist(string pathToNode)
    {
        IUANode myFolder = null;

        if (pathToNode != null)
        {
            myFolder = Project.Current.Get(pathToNode);
            if (myFolder == null)
            {
                string[] myNodeNames = pathToNode.Split('/');
                if (myNodeNames != null)
                {
                    var myRootNode = Project.Current.Get(myNodeNames[0]);
                    if (myRootNode != null)
                    {
                        var myParentNodePath = myNodeNames[0];
                        var myNewNodePath = myParentNodePath;
                        for (int i = 1; i < myNodeNames.Length; i++)
                        {
                            myNewNodePath += '/' + myNodeNames[i];

                            var myNode = Project.Current.Get(myNewNodePath);
                            if (myNode == null)
                            {
                                // Create missing node
                                myNode = Project.Current.Get(myParentNodePath);
                                if (myNode != null)
                                {
                                    var myNewFolder = InformationModel.MakeObject<FTOptix.Core.Folder>(myNodeNames[i]);
                                    myNode.Add(myNewFolder);
                                }
                            }
                            myParentNodePath = myNewNodePath;
                        }
                        myFolder = Project.Current.Get(pathToNode);
                    }
                    else
                    {
                        Log.Error("Root node is missing: " + myNodeNames[0]);
                    }
                }
            }
        }
        return myFolder;
    }

    private void DeleteObjectIfExists(IUANode project, string objectName)
    {
        if (project != null)
        {
            var myObject = project.Children.Get(objectName);
            if (myObject != null)
            {
                Log.Warning("Existing object is overwritten: " + objectName);
                myObject.Delete();
            }
        }
    }

    private void AddProperties(IUANode facePlate, CtOUINode uiNode)
    {
        foreach (var fixedProperty in uiNode.FixedProperties)
        {
            AddFixedProperty(facePlate, fixedProperty.Key, fixedProperty.Value);
        }

        foreach (var phiLink in uiNode.PhiLinks)
        {
            AddPhiLinks(facePlate, phiLink.Key, phiLink.Value);
        }
    }

    private void AddFixedProperty(IUANode facePlate, string propertyName, string propertyValue)
    {
        try
        {
            IUAVariable myProperty = facePlate.GetVariable(propertyName);
            if (myProperty != null)
            {
                switch ((uint)myProperty.ActualDataType.Id)
                {
                    case DataTypes.BooleanId:
                        myProperty.Value = propertyValue.ToLower() == "true" || propertyValue.ToLower() == "yes";
                        break;
                    case DataTypes.NodeIdId:
                        var node = Project.Current.Get(propertyValue);
                        myProperty.Value = node.NodeId;
                        break;
                    default:
                        myProperty.Value = propertyValue;
                        break;
                }
            }
            else
            {
                Log.Error(facePlate.BrowseName + " Missing property: " + propertyName);
            }

        }
        catch (Exception ex)
        {
            Log.Error("Exception when adding fixed properties: ", ex.Message);
        }
    }

    private void AddPhiLinks(IUANode facePlate, string propertyName, string propertyValue)
    {
        try
        {
            IUAVariable myProperty = facePlate.GetVariable(propertyName);
            if (myProperty != null)
            {
                var myVariable = Project.Current.GetVariable(propertyValue);
                if (myVariable != null)
                {
                    switch ((uint)myProperty.DataType.Id)
                    {
                        case DataTypes.NodeIdId:
                            myProperty.Value = myVariable.NodeId;
                            break;
                        default:
                            myProperty.SetDynamicLink(myVariable, FTOptix.CoreBase.DynamicLinkMode.ReadWrite);
                            break;
                    }
                }
                else
                {
                    Log.Error(facePlate.BrowseName + " Missing Phi variable: " + propertyValue);
                }
            }
            else
            {
                Log.Error(facePlate.BrowseName + " Missing Phi property: " + propertyName);
            }
        }
        catch (Exception ex)
        {
            Log.Error("Exception when adding Phi links: ", ex.Message);
        }
    }


}
