#region Using directives
using System;
using System.Xml;
using UAManagedCore;
using FTOptix.Core;
using System.Collections.Generic;
using System.Linq;
#endregion

public class NavXmlReader
{
	public NavXmlReader(){}
	
	public List<TreeNode<NavItem>> ReadXml(RT_CoT_NavigationLogic NavLogic, ResourceUri XmlPath)
	{
		try
		{
			XmlDocument xml = new();
			xml.Load(XmlPath.Uri);
			List<TreeNode<NavItem>> _roots = new();
			foreach (XmlNode node in xml.DocumentElement.ChildNodes)
			{
				_roots.Add(GetTreeNodeFromXml(NavLogic, node));
			}
			return _roots;
		}
		catch (Exception ex)
		{
			Log.Error($"Error in ReadXML: {ex.Message}");
			return null;
		}
	}
	
	private TreeNode<NavItem> GetTreeNodeFromXml(RT_CoT_NavigationLogic NavLogic, XmlNode Node, TreeNode<NavItem> Parent= null)
	{
		TreeNode<NavItem> treeNode = new(Parent);
		treeNode.SetValue(GetNavItemFromXml(NavLogic, Node, treeNode));
		foreach(XmlNode childNode in Node.ChildNodes)
		{
			treeNode.AddChild(GetTreeNodeFromXml(NavLogic, childNode, treeNode));
			treeNode.Value.HasChildren = true;
		}
		return treeNode;
	}
	
	private NavItem GetNavItemFromXml(RT_CoT_NavigationLogic NavLogic, XmlNode Node, TreeNode<NavItem> TreeNode)
	{
		string IconPath = (Node.Attributes["IconPath"] != null) ? Node.Attributes["IconPath"].InnerText : "";
		string Title = (Node.Attributes["Title"] != null) ? Node.Attributes["Title"].InnerText : "";
		string ScreenPath = (Node.Attributes["ScreenPath"] != null) ? Node.Attributes["ScreenPath"].InnerText : "";
		return new NavItem(NavLogic, TreeNode, IconPath, Title, ConvertScreenPath(ScreenPath));
	}
	
	private string ConvertScreenPath(string screenPath)
	{
		if (string.IsNullOrEmpty(screenPath))
		{
			return string.Empty;
		}

		string[] parts = screenPath.Split('/');
		int uiIndex = Array.IndexOf(parts, "UI");

		return uiIndex >= 0 ? string.Join("/", parts.Skip(uiIndex)) : string.Empty;
	}
}
