namespace WebApplication1.Services;

public class NoOpApplicationTelemetry : IApplicationTelemetry
{
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
    }
}