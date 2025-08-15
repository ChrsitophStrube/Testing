namespace HmiTesting.PlaywrightTests;

using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using static HmiTesting.Core.Helpers.PlaywrightHelper;
using HmiTesting.OpcUa;
using System.Threading.Tasks;
using HmiTesting.Web.Navigation;
using HmiTesting.Web.Components;
using LibUA.Core;
using HmiTesting.Web.Pages;
using HmiTesting.Core.Helpers;

//[Ignore("Temp deactivated")]
public class ScreenshotAllPages : UiTestBase
{

    [Test]
    public async Task ScreenshotsOfAllPages()
    {
        ScreenshotOnFailureAttribute.SetPage(_page!);
        Dictionary<OpcPath, string> screens = [];
        screens = NavigationPaser.GetScreensFromNavigationXML(@"D:\13_Masterarbeit\Repos\MAShmi\MAS-HMI\ProjectFiles\NavigationContent.xml");


        var results = await _session.Navigator(_page).GoToAllPages(screens, MakePageScreenshot);

    }

}