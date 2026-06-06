using Microsoft.ApplicationInsights;

namespace WebApplication1.Services;

public class ApplicationInsightsTelemetry : IApplicationTelemetry
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IHostEnvironment _hostEnvironment;

    public ApplicationInsightsTelemetry(TelemetryClient telemetryClient, IHostEnvironment hostEnvironment)
    {
        _telemetryClient = telemetryClient;
        _hostEnvironment = hostEnvironment;
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        var mergedProperties = properties == null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(properties);

        mergedProperties["environment"] = _hostEnvironment.EnvironmentName;
        mergedProperties["application"] = _hostEnvironment.ApplicationName;

        _telemetryClient.TrackEvent(eventName, mergedProperties, metrics);
    }
}