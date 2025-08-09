public sealed record ProjectConfig(
    string  ProjectName,
    int     OpcUaPort,
    string  OpcUaIp,
    string  ProjectUrl)
{
    /// <summary>
    /// Main Project settings for Connection
    /// </summary>
    public static ProjectConfig Current { get; } = new(
        ProjectName : "MyHMI_Template_Unencrypted",
        OpcUaPort   : 59100,
        OpcUaIp     : "192.168.0.1",
        ProjectUrl  : "http://192.168.0.1:8080/");
}