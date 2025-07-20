namespace HmiTesting.Core.Interfaces;

using static HmiTesting.Core.Helpers.PathHandler;

public interface INavigator
{
    INavigator ReturnToPreviousPage();
    INavigator ReturnToNextPage();
    Task<IHmiPage> GoToPage(OpcPath pagePath, string pageName);
}