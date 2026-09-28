namespace IoT.Domain.Entities.Devices;

public enum DoorState
{
    Closed = 0,
    Opening = 1,
    Open = 2,
    Closing = 3,
    Stopped = 4,
    Error = 5
}

public enum DeviceCommandType
{
    Open = 0,
    Close = 1,
    Stop = 2,
    Vent = 3
}

public enum DeviceCommandStatus
{
    Pending = 0,
    Sent = 1,
    Acknowledged = 2,
    Failed = 3,
    Cancelled = 4
}

public enum DeviceEventType
{
    DoorOpened = 0,
    DoorClosed = 1,
    ObstacleDetected = 2,
    ObstacleCleared = 3,
    AutoCloseTriggered = 4,
    ScheduleTriggered = 5,
    ManualOpen = 6,
    ManualClose = 7
}

public enum DeviceEventSource
{
    Remote = 0,
    LocalRfid = 1,
    Schedule = 2,
    AutoClose = 3,
    System = 4
}

/// <summary>
/// What kind of client last authenticated as the device.
///
/// The simulator announces itself with an X-Device-Client header; the firmware sends
/// nothing, so silence means real hardware. This exists so the UI can say which one it
/// is honestly instead of always claiming an ESP32 is attached.
/// </summary>
public enum DeviceClientKind
{
    /// <summary>Nothing has reported in yet.</summary>
    Unknown = 0,

    /// <summary>The ESP32 firmware, which sends no client header.</summary>
    Hardware = 1,

    /// <summary>tools/device-simulator, standing in for the disassembled board.</summary>
    Simulator = 2
}
