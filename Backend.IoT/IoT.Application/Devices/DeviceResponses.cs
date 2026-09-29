namespace IoT.Application.Devices;

/// <summary>
/// The pending-command payload the ESP32 reads.
///
/// <see cref="CommandType"/> is an int, not the enum, on purpose. The firmware parses it
/// with <c>doc["commandType"] | -1</c> and compares against integer literals, so this is a
/// frozen wire contract: the hardware can no longer be reflashed. Keeping the cast here
/// means a future global JsonStringEnumConverter could not silently break the device.
/// </summary>
public record PendingCommandResponse(Guid Id, int CommandType, int? TargetPercentage);

/// <summary>
/// One row of the event log. Type and Source are strings here, unlike the integer command
/// type above, because this is read by the dashboard rather than the firmware and the names
/// are what it displays.
/// </summary>
public record DeviceEventResponse(
    Guid Id,
    string EventType,
    string Source,
    DateTime Timestamp,
    string? Details);
