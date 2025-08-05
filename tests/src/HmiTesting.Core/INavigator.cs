namespace HmiTesting.Core.Interfaces;

using static HmiTesting.Core.Helpers.PathHandler;

public interface INavigator
{
    INavigator ReturnToPreviousPage();
    INavigator ReturnToNextPage();
    Task<IHmiPage> GoToPage(OpcPath pagePath, string pageName, bool bypassRestriction = false);
    Task<IReadOnlyList<Exception>> GoToAllPages(Dictionary<OpcPath, string> screens, Func<IHmiPage, string, Task> perPage, bool bypassRestriction = true);
}