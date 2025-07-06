namespace CoT
{
    public enum AlarmState
    {
        Reset = 0,
        Set = 1
    }
    
    public enum MessageType
    // Attention! MessageType enumeration values must match the MessageType enumeration values defined in Optix.
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Action = 3,
        Fault = 4,
        CriticalFault = 5
    }
}
