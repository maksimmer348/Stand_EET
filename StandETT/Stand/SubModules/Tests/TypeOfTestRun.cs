namespace StandETT;

/// <summary>
/// Тип текущего теста
/// </summary>
public enum TypeOfTestRun
{
    None = 0,
    Stopped,
    Stop,
    StopError,
    PrimaryCheckDevices,
    PrimaryCheckDevicesReady,
    CheckPortsReady,
    CheckPorts,
    
    WriteDevicesCmd = 10,
    WriteDevicesCmdReady = 11,
    
    PrimaryCheckVips,
    PrimaryCheckVipsReady,
    
    RelaySwitch,
    RelaySwitchReady,
    
    DeviceOperation,
    DeviceOperationReady,
    
    AvailabilityCheckVip,
    AvailabilityCheckVipReady,
    
    MeasurementZero,
    MeasurementZeroReady,
    
    WaitSupplyMeasurementZero,
    WaitSupplyMeasurementZeroReady,
    
    WaitHeatPlate,
    WaitHeatPlateReady,
    
    SmallLoadOutput,
    SmallLoadOutputReady,
    
    CycleCheck,
    CycleMeasurement,
    CyclicMeasurementReady,
    CycleWait,
    
    Error,
    OutputDevice,
    OutputDeviceReady
}