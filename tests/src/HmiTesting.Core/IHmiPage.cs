using LibUA.Core;
using Microsoft.Playwright;

namespace HmiTesting.Core.Interfaces;

public interface IHmiPage
{
    public IPage Page { get; }
    public NodeId PageId { get; }
    public IHmiPageArea GetAreaLayoutContentA(int number);
    public IHmiPageArea GetAreaLayoutContentB(int number);
    public IHmiPageArea GetAreaLayoutContentC(int number);
    public IHmiPageArea GetAreaLayoutListA(int number);
    public IHmiPageArea GetAreaLayoutListB(int number);
    public IHmiPageArea GetAreaLayoutListC(int number);
    public IHmiPageArea GetAreaLayoutListD(int number);
    public IReadOnlyList<IHmiPageArea>? GetAreasByLayout(string layout);
}