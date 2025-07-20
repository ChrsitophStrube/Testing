namespace HmiTesting.Core.Interfaces;

public interface IOpcUaClient
{
    IOpcUaSession Connect(string optixProjectName, string targetIP = "localhost", int port = 59100);
}