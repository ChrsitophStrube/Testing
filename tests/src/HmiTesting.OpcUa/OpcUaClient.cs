using LibUA;
using LibUA.Core;
namespace HmiTesting.OpcUa;

public class OpcUaClient 
{
    private static string _OptixProjectName;

    private static Client _client;

    public OpcUaSession Connect(string optixProjectName,string targetIP = "localhost", int port = 59100)
    {
        _OptixProjectName = optixProjectName;
        // Create Client
        _client = new Client(targetIP, port, 1000);


        // Connect to server
        if (_client.Connect() != StatusCode.Good)
        {
            throw new Exception($"can not connect to OPC-UA Server: {targetIP}:{port}");
        }

        // Open Secure channel without securety
        _client.OpenSecureChannel(
            MessageSecurityMode.None,
            SecurityPolicy.None,
            null);

        // Create Session
        var appDesc = new ApplicationDescription(
            "urn:MyApp",
            "http://mycompany.com/MyApp",
            new LocalizedText("My OPC UA Client"),
            ApplicationType.Client,
            null, null, null);
        _client.CreateSession(appDesc, "urn:MyApp", 120);

        // Activate Session
        var resActivate = _client.ActivateSession(
                new UserIdentityAnonymousToken("Anonymous"),
                new[] { "en-US" });
        if (resActivate != StatusCode.Good)
        {
            throw new Exception($"can not activate Session to OPC UA Server");
        }

       
        return new OpcUaSession(_client, _OptixProjectName);
}
}
