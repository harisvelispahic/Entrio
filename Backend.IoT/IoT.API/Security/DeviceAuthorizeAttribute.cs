using IoT.Application.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace IoT.API.Security;

/// <summary>
/// Authenticates the ESP32 by its X-Device-Key header and puts the resolved device in
/// HttpContext.Items["Device"], so device endpoints never take a device id from the body.
/// </summary>
public class DeviceAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Device-Key";

    /// <summary>
    /// Optional header a non-hardware client uses to identify itself. The firmware sends
    /// none, so its absence means real hardware.
    /// </summary>
    public const string ClientHeaderName = "X-Device-Client";

    public const string DeviceItemKey = "Device";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var devices = context.HttpContext.RequestServices.GetRequiredService<IDeviceService>();

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var key))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Request.Headers.TryGetValue(ClientHeaderName, out var client);

        var device = await devices.AuthenticateAsync(
            key!, client.FirstOrDefault(), context.HttpContext.RequestAborted);

        if (device is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[DeviceItemKey] = device;

        await next();
    }
}
