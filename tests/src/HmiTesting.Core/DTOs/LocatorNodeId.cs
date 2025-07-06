
using LibUA.Core;
using Microsoft.Playwright;

namespace HmiTesting.Core.DTOs
{
    public class LocatorNodeId
    {
        public NodeId NodeId { get; set; }
        public ILocator Locator { get; set; }
    }
}