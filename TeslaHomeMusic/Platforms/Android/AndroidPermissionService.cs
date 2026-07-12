using Android.Content.PM;
using Android.OS;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android;

public sealed class AndroidPermissionService : IPermissionService
{
	private const int RequestCode = 4201;

	public Task<bool> RequestAutomationPermissionsAsync()
	{
		var activity = Platform.CurrentActivity;
		if (activity is null || Build.VERSION.SdkInt < BuildVersionCodes.M)
		{
			return Task.FromResult(activity is not null);
		}

		var permissions = RequiredPermissions().Where(permission =>
			activity.CheckSelfPermission(permission) != Permission.Granted).ToArray();

		if (permissions.Length > 0)
		{
			activity.RequestPermissions(permissions, RequestCode);
		}

		var granted = RequiredPermissions().All(permission =>
			activity.CheckSelfPermission(permission) == Permission.Granted);

		return Task.FromResult(granted);
	}

	private static IEnumerable<string> RequiredPermissions()
	{
	#if AUTOMATION_APP
		yield return "android.permission.ACCESS_FINE_LOCATION";
		yield return "android.permission.ACCESS_COARSE_LOCATION";
		yield return "android.permission.BLUETOOTH_CONNECT";
		yield return "android.permission.BLUETOOTH_SCAN";
	#else
		yield return "android.permission.RECORD_AUDIO";
	#endif

		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			yield return "android.permission.POST_NOTIFICATIONS";
		}
	}
}
