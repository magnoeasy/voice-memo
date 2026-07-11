using TeslaHomeMusic.Models;

namespace TeslaHomeMusic.Services;

public interface ISettingsStore
{
	AppSettings Load();

	void Save(AppSettings settings);

	string LastStatus { get; }

	void SetStatus(string status);
}
