#region Using Directives
using System;
using System.Collections.Generic;
#endregion

/// <summary>
/// A class used to save the navigation histroy and go back and forth.
/// </summary>
public class NavHistory
{
	private List<NavItem> _history;
	private int _currentIndex;

	/// <summary>
	/// Simple constructor to initialize a new NavHistory isntance.
	/// </summary>
	public NavHistory()
	{
		_history = new List<NavItem>();
		_currentIndex = -1;
	}

	/// <summary>
	/// Clear the navigation history.
	/// </summary>
	public void ClearHistory()
	{
		_history.Clear();
		_currentIndex = -1;
	}

	/// <summary>
	/// Check if going back is possible.
	/// </summary>
	/// <returns>True = there is more entries from before. False = can't go back any further.</returns>
	public bool CanGoBack()
	{
		return _currentIndex > 0;
	}

	/// <summary>
	/// Check is going forward is possible.
	/// </summary>
	/// <returns>True = there is more entries afterwards. False = can't go forward any further.</returns>
	public bool CanGoForward()
	{
		return _currentIndex < _history.Count - 1;
	}

	/// <summary>
	/// Open a new screen and add it to the history.
	/// </summary>
	/// <param name="item">NavItem that is opened.</param>
	public void OpenScreen(NavItem item)
	{
		if (CanGoForward())
		{
			_history.RemoveRange(_currentIndex + 1, _history.Count - (_currentIndex + 1));
		}
		_history.Add(item);
		_currentIndex++;
	}

	/// <summary>
	/// Go back and get the previously opened NavItem.
	/// </summary>
	/// <returns>Last opened NavItem.</returns>
	public NavItem GoBack()
	{
		if (!CanGoBack()) { return null; }

		_currentIndex--;
		return _history[_currentIndex];
	}

	/// <summary>
	/// Go forward and get the next opened NavItem.
	/// </summary>
	/// <returns>Next opened NavItem.</returns>
	public NavItem GoForward()
	{
		if (!CanGoForward()) { return null; }

		_currentIndex++;
		return _history[_currentIndex];
	}

	public NavItem GetCurrentScreen()
	{
		return _currentIndex >= 0 ? _history[_currentIndex] : null;
	}
}
