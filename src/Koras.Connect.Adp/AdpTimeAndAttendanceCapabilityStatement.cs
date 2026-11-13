namespace Koras.Connect.Adp;

/// <summary>Explicit capability-report statement for ADP-016, so a host building a capability report has a single
/// citable fact instead of inferring scope from <see cref="AdpOperation"/>'s absence of attendance/timecard
/// values. This SDK exposes only the two documented time-off read operations
/// (<see cref="AdpOperation.TimeOffBalances"/>/<see cref="AdpOperation.TimeOffRequests"/>); attendance and
/// timecard operations are intentionally not implemented and are unverified against any vendor contract pending
/// independent research &#8212; a deliberate scope boundary, not an oversight or an accidental gap.</summary>
public static class AdpTimeAndAttendanceCapabilityStatement
{
    public static bool TimeOffReadsImplemented => true;
    public static bool AttendanceOperationsImplemented => false;
    public static bool TimecardOperationsImplemented => false;

    public static string Statement =>
        "This SDK implements ADP time-off balance and time-off request reads only " +
        "(AdpOperation.TimeOffBalances / AdpOperation.TimeOffRequests). Attendance and timecard operations are " +
        "NOT implemented and are unverified against any vendor contract, pending independent research (ADP-016); " +
        "their absence from the AdpOperation catalog is a deliberate scope boundary, not an oversight.";
}
