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

        var device = await devices.AuthenticateAsync(key!, context.HttpContext.RequestAborted);

        if (device is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items[DeviceItemKey] = device;

        await next();
    }
}
