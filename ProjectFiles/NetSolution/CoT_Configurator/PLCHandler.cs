using FTOptix.CommunicationDriver;
using FTOptix.Core;
using FTOptix.HMIProject;
using FTOptix.Modbus;
using FTOptix.NetLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;


class PLCHandler
{
// Completly unused right now since we cannot browse or create tags simply right now if we need to find a working solution we will revisit this function
    public void CreatePLCTags(List<string> plcTags)
    {


    }

    public static List<string> GetTagsFromTreeNode(TreeNode<CtOUINode> node)
    {
        List<CtOUINode> values = node.GetDescendantsValues();
        List<string> tags = [];
        foreach (var val in values)
        {
            tags.AddRange(val.PhiLinks.Values);
        }
        return tags;
    }

    /// <summary>
    /// Filter tags to import by tag name.
    /// Expects a list of tag paths to import (like "Controller Tags/Motor1/Speed" and
    /// the list of all tags that were fetch from the PLC.
    /// If the list of tags to import is empty, all fetched tags will be returned.
    /// Warning: a flat list is returned, so the tag paths must be unique.
    /// </summary>
    /// <param name="tagPathsToImport">List of tag paths to import (path with forward slashes "/")</param>
    /// <param name="plcItems">List of all tags fetched from the PLC</param>
    /// <returns>List of tags (Struct[]) that matches the input list</returns>
    private static Struct[] FilterTagsToImport(List<string> tagPathsToImport, Struct[] plcItems)
    {
        var listOfFoundTags = new List<Struct>();

        // If no list of tags to import was provided, return all tags
        if (tagPathsToImport.Count == 0)
        {
            Log.Warning("RuntimeTagsImport.FilterTagsToImport", "No list of tags to import was provided. Returning all tags.");
            return plcItems;
        }

        if (plcItems.Length == 0)
        {
            Log.Warning("RuntimeTagsImport.FilterTagsToImport", "No tags were fetched from the PLC. Returning empty list.");
            return plcItems;
        }

        // Filter tags to import by tag name
        foreach (string tagPath in tagPathsToImport)
        {
            string[] splitPath = tagPath.Split('/');
            var node = plcItems;
            for (int i = 0; i < splitPath.Length; i++)
            {
                for (int k = 0; k < node.Length; k++)
                {
                    if (((BasePlcItem)node[k]).Name == splitPath[i])
                    {
                        if (i == splitPath.Length - 1)
                        {
                            listOfFoundTags.Add(node[k]);
                            break;
                        }
                        else
                        {
                            node = ((BasePlcItem)node[k]).Items;
                            break;
                        }
                    }
                }
            }
        }

        return listOfFoundTags.ToArray();
    }
}
