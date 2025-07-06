using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Xml;
using FTOptix.UI;
using UAManagedCore;

/**
This class will parse a given XML and create the optix object that need to be created or instantiated.

The input will be a mereged XML file fromt he DT_Netlogic and the class will create model tree out of it. This tree will then be passed back and used by the Object Creator to create the objects needed from the Configurator

Additionally this class will parse XMLs that are there for general structures

Navigation XML pruning is an additional task that could be part of this class at some point

*/
class CtoXmlParser
{

    private XmlDocument _currentDocument;
    private XmlNode _currentModule;
    private int _objectId = 0;
    public CtoXmlParser(string xmlPath)
    {
        _currentDocument = new();
        _currentDocument.Load(xmlPath);
    }
    private CtOUINode CreateAlarmNode(XmlElement xmlNode, string idPrefix)
    {
        CtOUINode node = new CtOUINode();
        node.Name = xmlNode.Attributes["TagName"]?.Value;
        if (node.Name == null || node.Name == string.Empty)
        {
            node.Name = idPrefix + xmlNode.Attributes["id"].Value;
        }
        foreach (XmlAttribute attr in xmlNode.Attributes)
        {
            if (attr.Name == "id")
                addPropery(node, attr.Name, idPrefix + attr.Value);
            else addPropery(node, attr.Name, attr.Value);

        }
        return node;
    }

    private CtOUINode CreateGenericObjectNode(XmlElement xmlNode, XmlElement module)
    {
        CtOUINode node = new CtOUINode();
        string plcPrefix = GetPlcPrefix(module);

        if (xmlNode.HasAttribute("Type") && xmlNode.Attributes["Type"].Value == "FacePlate")
        {
            node = ResolveFacePlateLink(xmlNode, plcPrefix);
        }
        else
        {
            node.Name = xmlNode.Attributes["TagName"]?.Value;
            if (node.Name == null || node.Name == string.Empty)
            {
                node.Name = xmlNode.Attributes["id"].Value;
            }
            foreach (XmlAttribute attr in xmlNode.Attributes)
            {
                addPropery(node, attr.Name, attr.Value);
                // node.FixedProperties.Add(attr.Name, attr.Value);
            }
        }
        return node;

    }

    private void addPropery(CtOUINode node, string propName, string propValue)
    {
        node.FixedProperties.Add(propName, propValue);
    }

    public TreeNode<CtOUINode> CreateAlarmModel()
    {
        XmlNodeList modules = _currentDocument.SelectNodes("//PLCIface");
        var root = new TreeNode<CtOUINode>(new CtOUINode());
        foreach (XmlElement module in modules)
        {
            root.AddChild(CreateTreeFromModule(module, "Additive/Alarms/Alarm", "Alarms/PlcAlarms", false, "CoT_PlcAlarm"));
        }
        return root;
    }
    public TreeNode<CtOUINode> CreateCfgModel()
    {
        XmlNodeList modules = _currentDocument.SelectNodes("//PLCIface");
        var root = new TreeNode<CtOUINode>(new CtOUINode());
        foreach (XmlElement module in modules)
        {
            root.AddChild(CreateTreeFromModule(module, "Additive/Cfgs/Cfg", GetModuleCFGPath(module), false, "CtO_cfgBaseType"));
        }
        return root;
    }

    private TreeNode<CtOUINode> CreateTreeFromModule(XmlElement mod, string xmlNodePath, string optixPathPrefix, bool isScreen, string optixType = "default")
    {
        TreeNode<CtOUINode> moduleRoot = new(new CtOUINode(GetModuleName(mod)));
        _currentModule = mod;
        string plcPrefix = GetPlcPrefix(mod);
        XmlNodeList moduleNodes = _currentModule.SelectNodes(xmlNodePath);

        foreach (XmlElement currNode in moduleNodes)
        {
            //Faceplates
            if (currNode.HasAttribute("Type") && currNode.Attributes["Type"].Value == "FacePlate")
            {
                CtOUINode facePlateScreen = ResolveFacePlateLink(currNode, plcPrefix);
                facePlateScreen.IsNewOptixType = isScreen;
                facePlateScreen.PathToNode = $"{optixPathPrefix}/{GetModuleName(mod)}";
                facePlateScreen.PlcPathPrefix = plcPrefix;
                moduleRoot.AddChild(new TreeNode<CtOUINode>(facePlateScreen));
            }
            // Addtive Screens
            else
            {
                if (isScreen)
                {
                    TreeNode<CtOUINode> currentScreen = CreateAddtitiveScreen(currNode, $"{optixPathPrefix}/{GetModuleName(mod)}", plcPrefix);
                    currentScreen.Value.PlcPathPrefix = plcPrefix;
                    moduleRoot.AddChild(currentScreen);
                }
                else
                {
                    var treeNode = new TreeNode<CtOUINode>(CreateGenericObjectNode(currNode, mod));
                    if (optixType == "CoT_PlcAlarm")
                        treeNode = new TreeNode<CtOUINode>(CreateAlarmNode(currNode, GetPrefixIds(mod)));
                    treeNode.Value.OptixType = optixType;
                    treeNode.Value.PathToNode = $"{optixPathPrefix}/{GetModuleName(mod)}";
                    treeNode.Value.PlcPathPrefix = plcPrefix;
                    moduleRoot.AddChild(treeNode);
                }
            }

        }
        return moduleRoot;
    }
    public TreeNode<CtOUINode> CreateScreenModel()
    {

        var root = new TreeNode<CtOUINode>(new CtOUINode());

        XmlNodeList modules = _currentDocument.SelectNodes("//PLCIface");
        foreach (XmlElement module in modules)
        {
            root.AddChild(CreateTreeFromModule(module, "Additive/Screens/Screen", GetModuleScreenPath(module), true));
        }
        return root;
    }


    private TreeNode<CtOUINode> CreateAddtitiveScreen(XmlElement screen, string parentPath, string plcPrefix)
    {
        CtOUINode screenRootNode = new();
        screenRootNode.Name = screen.Attributes["Name"].Value;
        screenRootNode.OptixType = screen.Attributes["Type"].Value;
        screenRootNode.PathToNode = parentPath;
        screenRootNode.IsNewOptixType = true;
        screenRootNode.PlcPathPrefix = plcPrefix;
        TreeNode<CtOUINode> screenRoot = new(screenRootNode);
        parentPath = string.Format("{0}/{1}", parentPath, screenRootNode.Name); // $"{parentPath}/{screenRootNode.Name}"
        foreach (XmlElement child in screen.ChildNodes)
        {
            screenRoot.AddChild(CreateSubTree(child, parentPath, plcPrefix));
        }
        return screenRoot;
    }
    private string GetPlcPrefix(XmlElement mod)
    {
        XmlElement plcPath = (XmlElement)mod.SelectSingleNode("GlobalInformation/MetaInfo[@Name='PLCPath']");
        return plcPath?.InnerText ?? "";
    }

    private string GetPrefixIds(XmlElement mod)
    {
        int levels = 1;
        XmlElement lastId = (XmlElement)mod.SelectSingleNode("GlobalInformation/MetaInfo[@Name='LevelID1']");
        string value = "";
        while (lastId != null || levels > 4)
        {
            value += $"{lastId.InnerText}.";
            levels++;
            lastId = (XmlElement)mod.SelectSingleNode($"GlobalInformation/MetaInfo[@Name='LevelID{levels}']");
        }
        return value;
    }
    private string GetModuleScreenPath(XmlElement currModule)
    {
        XmlElement metaInfoScreenPath = (XmlElement)currModule.SelectSingleNode("GlobalInformation/MetaInfo[@Name='DefaultScreenPath']");
        return metaInfoScreenPath?.InnerText ?? "";
    }

    private string GetModuleCFGPath(XmlElement currModule)
    {
        XmlElement metaInfoScreenPath = (XmlElement)currModule.SelectSingleNode("GlobalInformation/MetaInfo[@Name='DefaultCFGPath']");
        return metaInfoScreenPath?.InnerText ?? "Recipes/UNKOWNMODULE";
    }    private string GetModuleName(XmlElement currModule)
    {
        XmlElement metaInfoName = (XmlElement)currModule.SelectSingleNode("GlobalInformation/Name");
        return metaInfoName.InnerText;
    }

    private TreeNode<CtOUINode> CreateSubTree(XmlElement node, string parentPath, string plcPath)
    {
        CtOUINode currNode = new(node.Name);
        currNode.PlcPathPrefix = plcPath;
        TreeNode<CtOUINode> currSubtree = new(currNode);
        if (node.Name == "FacePlate")
        {
            currNode = ResolveFacePlateLink(node, plcPath);
            currNode.PathToNode = parentPath;
            currSubtree = new(currNode);
        }
        else if (node.Name == "Structure")
        {
            currNode.Name = node.Attributes["Name"].Value;
            currNode.PathToNode = $"{parentPath}/{node.Attributes["PathToNode"].Value}/{node.Attributes["Name"].Value}";
            foreach (XmlElement child in node.ChildNodes)
            {
                currSubtree.AddChild(CreateSubTree(child, currNode.PathToNode, plcPath));
            }
        }
        else
        {
            currNode.Name = $"{node.Name}_{_objectId++}";
            currNode.OptixType = node.Name;
            currNode.PathToNode = parentPath;
            foreach (XmlAttribute attr in node.Attributes)

            {
                addPropery(currNode, attr.Name, attr.Value);
                // currNode.FixedProperties.Add(attr.Name, attr.Value);
            }
            foreach (XmlElement child in node.ChildNodes)
            {
                currSubtree.AddChild(CreateSubTree(child, $"{parentPath}/{currNode.Name}", plcPath));
            }
        }
        return currSubtree;
    }
    private CtOUINode ResolveFacePlateLink(XmlElement node, string plcPrefix)
    {
        CtOUINode newFaceplate = new();
        XmlNode facplateDefinition = _currentModule.SelectSingleNode($"FacePlates/FacePlate[@Name='{node.Attributes["FacePlateName"].Value}']");
        newFaceplate.Name = node.Attributes["FacePlateName"].Value;
        foreach (XmlElement child in facplateDefinition)
        {
            if (child.Name == "Type")
            {
                newFaceplate.OptixType = child.InnerText;
            }
            if (child.Name == "Phi")
            {
                newFaceplate.PhiLinks.Add(child.Attributes["Name"].Value, $"{plcPrefix}/{child.InnerText}");
            }
            if (child.Name == "Parameter")
            {
                addPropery(newFaceplate, child.Attributes["Name"].Value, child.InnerText);
                // newFaceplate.FixedProperties.Add(child.Attributes["Name"].Value, child.InnerText);
            }
            if (child.Name == "ScreenLink")
            {
                newFaceplate.ScreenLinks.Add(child.Attributes["Name"].Value, child.InnerText);
            }
        }
        return newFaceplate;
    }
}
