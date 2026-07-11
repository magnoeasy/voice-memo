using Android.AccessibilityServices;
using Android.App;
using Android.OS;
using Android.Views.Accessibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using TeslaHomeMusic.Services;

namespace TeslaHomeMusic.Platforms.Android.ChargePoint;

[Service(Exported = true, Permission = "android.permission.BIND_ACCESSIBILITY_SERVICE")]
[IntentFilter(["android.accessibilityservice.AccessibilityService"])]
[MetaData("android.accessibilityservice", Resource = "@xml/chargepoint_accessibility_service")]
public sealed class ChargePointAccessibilityService : AccessibilityService
{
	private const string ChargePointPackageName = "com.coulombtech";
	private static readonly string[] AlertLabels = ["Notify", "Alert"];
	private DateTimeOffset lastStationSearch;
	private DateTimeOffset lastAlertActivation;
	private bool stationSelected;

	public override void OnAccessibilityEvent(AccessibilityEvent? e)
	{
		if (!string.Equals(e?.PackageName?.ToString(), ChargePointPackageName, StringComparison.Ordinal))
		{
			stationSelected = false;
			return;
		}

		var settings = IPlatformApplication.Current?.Services
			?.GetService<ISettingsStore>()
			?.Load();
		if (settings is null ||
			!settings.IsChargePointAlertAutomationEnabled ||
			string.IsNullOrWhiteSpace(settings.ChargePointStationName))
		{
			return;
		}

		var root = RootInActiveWindow;
		if (root is null)
		{
			return;
		}

		if (!stationSelected)
		{
			stationSelected = TrySelectStation(root, settings.ChargePointStationName);
			if (!stationSelected)
			{
				TrySearchForStation(root, settings.ChargePointStationName);
				return;
			}
		}

		if (DateTimeOffset.UtcNow - lastAlertActivation >= TimeSpan.FromMinutes(2) &&
			TryActivateAlert(root))
		{
			lastAlertActivation = DateTimeOffset.UtcNow;
		}
	}

	public override void OnInterrupt()
	{
	}

	private static bool TrySelectStation(AccessibilityNodeInfo root, string stationName)
	{
		var matches = root.FindAccessibilityNodeInfosByText(stationName);
		if (matches is null)
		{
			return false;
		}

		foreach (var match in matches)
		{
			if (match.ViewIdResourceName?.Contains("map_search_text_field", StringComparison.Ordinal) == true)
			{
				continue;
			}

			if (TryClick(match))
			{
				return true;
			}
		}

		return false;
	}

	private void TrySearchForStation(AccessibilityNodeInfo root, string stationName)
	{
		if (DateTimeOffset.UtcNow - lastStationSearch < TimeSpan.FromSeconds(3))
		{
			return;
		}

		var searchFields = root.FindAccessibilityNodeInfosByViewId("map_search_text_field_title");
		var searchField = searchFields?.FirstOrDefault();
		if (searchField is null)
		{
			return;
		}

		var arguments = new Bundle();
		arguments.PutCharSequence(AccessibilityNodeInfo.ActionArgumentSetTextCharsequence, stationName);
		if (searchField.PerformAction(global::Android.Views.Accessibility.Action.SetText, arguments))
		{
			lastStationSearch = DateTimeOffset.UtcNow;
		}
	}

	private static bool TryActivateAlert(AccessibilityNodeInfo root)
	{
		foreach (var label in AlertLabels)
		{
			var matches = root.FindAccessibilityNodeInfosByText(label);
			if (matches is null)
			{
				continue;
			}

			foreach (var match in matches)
			{
				if (TryClick(match))
				{
					return true;
				}
			}
		}

		return false;
	}

	private static bool TryClick(AccessibilityNodeInfo node)
	{
		for (var target = node; target is not null; target = target.Parent)
		{
			if (target.Enabled && target.Clickable)
			{
				return target.PerformAction(global::Android.Views.Accessibility.Action.Click);
			}
		}

		return false;
	}
}
