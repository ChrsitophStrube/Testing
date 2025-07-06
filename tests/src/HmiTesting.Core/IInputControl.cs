using HmiTesting.Core.enums;

namespace HmiTesting.Core.Interfaces;

public interface IInputControl
{
    // public Task<bool> IsVisibleAsync();
    public Task<bool> IsEnabled();
    public string GetUserRole();
    public Task<bool> IsVisibleAsync(int timeoutMs );

}