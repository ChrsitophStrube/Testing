#region usings
using System;
using System.Collections.Generic;
using System.Linq;
#endregion

public class TreeNode<T>
{
	public T Value { get; set; }
	public TreeNode<T> Parent { get; set; }
	public List<TreeNode<T>> Children { get; set; }
	private List<TreeNode<T>> TreePath { get; set; }

	/// <summary>
	/// Constructor to create a new node of a generic tree
	/// </summary>
	/// <param name="Parent">Parent node. Is null for the root</param>
	public TreeNode(TreeNode<T> Parent)
	{
		this.Parent = Parent;
		Children = new();
		TreePath = new();
	}

	/// <summary>
	/// Constructor to create a new node of a generic tree
	/// </summary>
	/// <param name="Parent">Parent node. Is null for the root</param>
	/// <param name="Value">Value of the tree node</param>
	public TreeNode(TreeNode<T> Parent, T Value)
	{
		this.Value = Value;
		this.Parent = Parent;
		Children = new();
		TreePath = new();
	}
	public TreeNode(T Value)
	{
		this.Value = Value;
		Parent = null;
		Children = [];
		TreePath = [];
	}

	/// <summary>
	/// Set the value of the tree node
	/// </summary>
	/// <param name="Value"></param>
	public void SetValue(T Value)
	{
		this.Value = Value;
	}

	/// <summary>
	/// Add a child to the tree node.
	/// </summary>
	/// <param name="child">Node to add as a child.</param>
	public void AddChild(TreeNode<T> child)
	{
		child.Parent = this;
		Children.Add(child);
		child.ConstructTreePath();
	}

	/// <summary>
	/// Get the values of all child nodes of this node
	/// </summary>
	/// <returns>List of values</returns>
	public List<T> GetChildValues() => GetValues(Children);

	/// <summary>
	/// Get all nodes in the tree structure below this node
	/// </summary>
	/// <returns>List of nodes</returns>
	public List<TreeNode<T>> GetDescendants()
	{
		List<TreeNode<T>> temp = new();
		foreach (var child in Children)
		{
			temp.Add(child);
			temp.AddRange(child.GetDescendants());
		}
		return temp;
	}

	/// <summary>
	/// Get the values of all nodes in the tree structure below this node
	/// </summary>
	/// <returns>List of values</returns>
	public List<T> GetDescendantsValues() => GetValues(GetDescendants());

	/// <summary>
	/// Get all nodes in the tree structure below this node that are a leaf
	/// </summary>
	/// <returns>List of nodes</returns>
	public List<TreeNode<T>> GetLeafDescendants()
	{
		List<TreeNode<T>> leafs = new();
		foreach (var child in Children)
		{
			if (child.Children.Count == 0)
			{
				leafs.Add(child);
			}
			else
			{
				leafs.AddRange(child.GetLeafDescendants());
			}
		}
		return leafs;
	}

	/// <summary>
	/// Get all values of the nodes in the tree structure below this node that are a leaf
	/// </summary>
	/// <returns>List of values</returns>
	public List<T> GetLeafDescendantsValues() => GetValues(GetLeafDescendants());

	/// <summary>
	/// Get the root of the tree
	/// </summary>
	/// <returns>Root node</returns>
	public TreeNode<T> GetRoot() => GetTreePath()[0];

	/// <summary>
	/// Get the value of the root of the tree
	/// </summary>
	/// <returns>Root value</returns>
	public T GetRootValue() => GetRoot().Value;

	/// <summary>
	/// Get the value of this nodes parent node
	/// </summary>
	/// <returns>Parent value</returns>
	public T GetParentValue() => Parent.Value;

	/// <summary>
	/// Get all sibling nodes of this node
	/// </summary>
	/// <returns>List of nodes</returns>
	public List<TreeNode<T>> GetSiblings() => Parent == null ? new List<TreeNode<T>>() : Parent.Children.FindAll(child => child != this).ToList();

	/// <summary>
	/// Get all values of the sibling nodes of this node
	/// </summary>
	/// <returns></returns>
	public List<T> GetSiblingsValues() => GetValues(GetSiblings());

	/// <summary>
	/// Get the values of the path to this node in the tree starting with the root
	/// </summary>
	/// <returns>List of values</returns>
	public List<T> GetTreePathValues() => GetValues(GetTreePath());

	/// <summary>
	/// Get the level  where this node is in the tree structure
	/// </summary>
	/// <returns>Level</returns>
	public int GetTreeLevel() => GetTreePath().Count - 1;

	/// <summary>
	/// Get the path to this node in the tree starting with the root
	/// </summary>
	/// <returns>List of nodes</returns>
	public List<TreeNode<T>> GetTreePath()
	{
		if (TreePath.Count == 0)
		{
			ConstructTreePath();
		}
		return TreePath;
	}

	public List<T> GetPreOrderValues()
	{
		if (this == null)
		{
			return [];
		}
		Stack<TreeNode<T>> nodeStack = new();
		nodeStack.Push(GetRoot());
		List<T> preOrderList = [];
		while (nodeStack.Count > 0)
		{
			TreeNode<T> currNode = nodeStack.Pop();
			preOrderList.Add(currNode.Value);
			currNode.Children.Reverse();
			foreach (var child in currNode.Children)
			{
				nodeStack.Push(child);
			}
		}
		return preOrderList;
	}

	private void ConstructTreePath()
	{
		if (Parent == null)
		{
			TreePath = new List<TreeNode<T>>() { this };
		}
		else
		{
			List<TreeNode<T>> tmp = new();
			tmp.AddRange(Parent.GetTreePath());
			tmp.Add(this);
			TreePath = tmp;
		}
	}

	private List<T> GetValues(List<TreeNode<T>> nodeList)
	{
		List<T> temp = new();
		foreach (var node in nodeList)
		{
			temp.Add(node.Value);
		}
		return temp;
	}
}
