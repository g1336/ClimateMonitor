using ClimateMonitor.Services;
using ClimateMonitor.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClimateMonitor.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly DeviceSecretValidatorService _secretValidator;
    private readonly AlertService _alertService;

    public ReadingsController(
        DeviceSecretValidatorService secretValidator,
        AlertService alertService)
    {
        _secretValidator = secretValidator;
        _alertService = alertService;
    }

    /// <summary>
    /// Evaluate a sensor readings from a device and return possible alerts.
    /// </summary>
    /// <remarks>
    /// The endpoint receives sensor readings (temperature, humidity) values
    /// as well as some extra metadata (firmwareVersion), evaluates the values
    /// and generate the possible alerts the values can raise.
    /// 
    /// There are old device out there, and if they get a firmwareVersion 
    /// format error they will request a firmware update to another service.
    /// </remarks>
    /// <param name="deviceSecret">A unique identifier on the device included in the header(x-device-shared-secret).</param>
    /// <param name="deviceReadingRequest">Sensor information and extra metadata from device.</param>
    [HttpPost("evaluate")]
    public ActionResult<IEnumerable<Alert>> EvaluateReading(
        [FromHeader(Name = "x-device-shared-secret")] string deviceSecret,
        [FromBody] DeviceReadingRequest deviceReadingRequest)
    {
        if (!_secretValidator.ValidateDeviceSecret(deviceSecret))
        {
            return Problem(
                detail: "Device secret is not within the valid range.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var possibleFirmwareError = ValidateFirmwareVersionFormat(deviceReadingRequest);

        if (possibleFirmwareError != null)
            return BadRequest(possibleFirmwareError);

        return Ok(_alertService.GetAlerts(deviceReadingRequest));
    }

    private ValidationProblemDetails ValidateFirmwareVersionFormat(DeviceReadingRequest deviceReadingRequest)
    {
        if (_alertService.ValidateFirmwareFormat(deviceReadingRequest))
            return null;

        var validationProblems = new Dictionary<string, string[]>
            {
                { "FirmwareVersion", new string[] { "The firmware value does not match semantic versioning format." } },
            };

        var problemDetails = new ValidationProblemDetails(validationProblems)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation errors occurred",
            Instance = HttpContext.Request.Path
        };

        return problemDetails;
    }
}
