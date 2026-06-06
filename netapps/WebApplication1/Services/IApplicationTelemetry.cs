namespace WebApplication1.Services;

public interface IApplicationTelemetry
{
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
}