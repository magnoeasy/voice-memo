using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public interface IHomeArrivalMonitor
{
	void Apply(AppSettings settings);
}
