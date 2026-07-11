namespace TeslaHomeMusic.Services;

public interface IPermissionService
{
	Task<bool> RequestAutomationPermissionsAsync();
}
