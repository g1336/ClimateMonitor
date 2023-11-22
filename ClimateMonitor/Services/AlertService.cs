using ClimateMonitor.Services.Models;
using System.Text.RegularExpressions;

namespace ClimateMonitor.Services;

public class AlertService
{
    private static readonly HashSet<Func<DeviceReadingRequest, Alert?>> SensorValidators = new()
    {
        deviceReading =>
            deviceReading.Humidity is < 0 or > 100 
            ? new Alert(AlertType.HumiditySensorOutOfRange, "Humidity sensor is out of range.")
            : default,

        deviceReading => 
            deviceReading.Temperature is < -10 or > 50 
            ? new Alert(AlertType.TemperatureSensorOutOfRange, "Temperature sensor is out of range.")
            : default,
    };

    public IEnumerable<Alert> GetAlerts(DeviceReadingRequest deviceReadingRequest)
    {
        return SensorValidators
            .Select(validator => validator(deviceReadingRequest))
            .OfType<Alert>();
    }

    public bool ValidateFirmwareFormat(DeviceReadingRequest deviceReadingRequest)
    {
        if (deviceReadingRequest.FirmwareVersion == null) return false;

        Regex regex = new("^(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-\r\n]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9azA-Z-]+)*))?$");
        
        return regex.IsMatch(deviceReadingRequest.FirmwareVersion);
    }
}
