using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

[AttributeUsage(AttributeTargets.Assembly)]

public sealed class ScreenshotOnFailureAttribute : NUnitAttribute, ITestAction
{
    private static readonly AsyncLocal<IPage?> _currentPage = new();

    public static void SetPage(IPage page) => _currentPage.Value = page;

    public void BeforeTest(ITest _) { }

    public void AfterTest(ITest test)
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
            return;

        var page = _currentPage.Value;
        if (page == null) return;

        var file = Path.Combine(FindRepoRoot(TestContext.CurrentContext.WorkDirectory),
                                "test-results", "screenshots",
                                $"{Sanitize(test.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.png"); 
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        // --> 1. Nachricht ermitteln
        string msg = TestContext.CurrentContext.Result.Message ?? "(no message)";
        string trace = TestContext.CurrentContext.Result.StackTrace ?? "";
        string info = (msg + "\n" + trace).Trim();
        if (info.Length > 500) info = info[..500] + "…";

        // --> 2. Overlay injizieren
        page.EvaluateAsync(@"(html) => {
        const tag = Object.assign(document.createElement('pre'), {
            textContent: html,
            id: 'pw-error-banner',
            style: `
              position:fixed;bottom:0;left:0;right:0;max-height:40%;
              overflow:auto;padding:8px;background:#c00;color:#fff;
              font:12px/16px monospace;z-index:999999;white-space:pre-wrap;`
        });
        document.body.append(tag);
    }", info).GetAwaiter().GetResult();

        // --> 3. Screenshot
        page.ScreenshotAsync(new() { Path = file, FullPage = true })
            .GetAwaiter().GetResult();

        // --> 4. Aufräumen
        page.EvaluateAsync("() => document.getElementById('pw-error-banner')?.remove()")
            .GetAwaiter().GetResult();

        TestContext.AddTestAttachment(file, "Fehlerscreenshot");
    }


    public ActionTargets Targets => ActionTargets.Test;

    private static string Sanitize(string s) =>
        string.Concat(s.Split(Path.GetInvalidFileNameChars()));


    static string FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);

        while (dir != null)
        {
            // Kriterium: enthält .sln oder .git
            bool isRoot = dir.EnumerateFiles("*.sln").Any() ||
                          dir.GetDirectories(".git").Any();

            if (isRoot) return dir.FullName;

            dir = dir.Parent;          // eine Ebene höher
        }

        throw new DirectoryNotFoundException("Repo root not found");
    }

}