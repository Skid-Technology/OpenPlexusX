using System;
using System.Collections.Generic;
using System.Linq;

namespace VibranceHud.Monitors;

public sealed class MonitorSelection
{
	private readonly AppSettings _settings;

	private readonly Func<IReadOnlyList<ColourMonitor>> _inventory;

	private HashSet<string> _ambiguous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private const string DeviceKeyPrefix = "device:";

	private const string StableOptOutPrefix = "monitor:";

	private const string InventoryPrefix = "inventory:";

	public IReadOnlyList<ColourMonitorGroup> Groups { get; private set; } = Array.Empty<ColourMonitorGroup>();

	public bool AllowsMatrix => Groups.All(IsSelected);

	internal bool WorthShowing
	{
		get
		{
			if (Groups.Count < 2)
			{
				return Groups.Any((ColourMonitorGroup g) => !IsSelected(g));
			}
			return true;
		}
	}

	public event Action? Changed;

	public MonitorSelection(AppSettings settings, Func<IReadOnlyList<ColourMonitor>> inventory)
	{
		_settings = settings;
		_inventory = inventory;
		Refresh();
	}

	public void Refresh()
	{
		IReadOnlyList<ColourMonitor> source;
		try
		{
			source = _inventory();
		}
		catch
		{
			source = Array.Empty<ColourMonitor>();
		}
		_ambiguous = (from g in source.GroupBy((ColourMonitor x) => x.Identity, StringComparer.OrdinalIgnoreCase)
			where string.IsNullOrWhiteSpace(g.Key) || g.Select((ColourMonitor x) => x.DeviceName).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1
			select g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
		Groups = (from g in source.GroupBy((ColourMonitor x) => x.DeviceName, StringComparer.OrdinalIgnoreCase)
			select new ColourMonitorGroup(g.Key, g.Select((ColourMonitor x) => x.Identity).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy((string x) => x, StringComparer.OrdinalIgnoreCase)
				.ToArray(), string.Join(" + ", g.Select((ColourMonitor x) => x.Name).Distinct()), g.First().Bounds, g.Any((ColourMonitor x) => x.Primary), g.First().RefreshHz) into x
			orderby x.Primary descending, x.Bounds.X, x.Bounds.Y
			select x).ThenBy((ColourMonitorGroup x) => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();
		PreserveOptOuts();
		Changed?.Invoke();
	}

	private void PreserveOptOuts()
	{
		List<string> untickedDeviceKeys = _settings.UntickedDeviceKeys;
		if (untickedDeviceKeys == null || untickedDeviceKeys.Count <= 0)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(untickedDeviceKeys, StringComparer.OrdinalIgnoreCase);
		foreach (ColourMonitorGroup item2 in Groups.Where(Resolved))
		{
			if (hashSet.Remove("device:" + item2.DeviceName) || hashSet.Remove(item2.DeviceName))
			{
				hashSet.Add("monitor:" + item2.Identity);
			}
		}
		HashSet<string> current2 = Groups.Select((ColourMonitorGroup g) => g.DeviceName).ToHashSet(StringComparer.OrdinalIgnoreCase);
		bool flag = hashSet.Any((string k) => (k.StartsWith("device:", StringComparison.OrdinalIgnoreCase) && !current2.Contains(k.Substring("device:".Length))) || ((k.StartsWith("DISPLAY", StringComparison.OrdinalIgnoreCase) || k.StartsWith("\\\\.\\DISPLAY", StringComparison.OrdinalIgnoreCase)) && !current2.Contains(k)));
		string item = InventoryKey();
		bool flag2 = hashSet.Any((string k) => k.StartsWith("inventory:", StringComparison.OrdinalIgnoreCase)) && !hashSet.Contains(item) && Groups.Any((ColourMonitorGroup g) => !Resolved(g)) && hashSet.Any((string k) => k.StartsWith("device:", StringComparison.OrdinalIgnoreCase));
		if ((flag | flag2) && !hashSet.Contains(item))
		{
			hashSet.RemoveWhere((string k) => k.StartsWith("inventory:", StringComparison.OrdinalIgnoreCase));
			foreach (ColourMonitorGroup item3 in Groups.Where((ColourMonitorGroup g) => !Resolved(g)))
			{
				hashSet.Add("device:" + item3.DeviceName);
			}
			hashSet.Add(item);
		}
		_settings.UntickedDeviceKeys = ((hashSet.Count == 0) ? null : hashSet.OrderBy((string k) => k, StringComparer.OrdinalIgnoreCase).ToList());
	}

	private string InventoryKey()
	{
		return "inventory:" + string.Join("|", Groups.Select((ColourMonitorGroup g) => g.DeviceName + ":" + g.Identity).OrderBy((string k) => k, StringComparer.OrdinalIgnoreCase));
	}

	private bool Resolved(ColourMonitorGroup group)
	{
		if (group.Identities.Length != 0)
		{
			return group.Identities.All((string id) => !string.IsNullOrWhiteSpace(id) && !_ambiguous.Contains(id));
		}
		return false;
	}

	public string GroupKey(ColourMonitorGroup group)
	{
		if (!Resolved(group))
		{
			return "device:" + group.DeviceName;
		}
		return group.Identity;
	}

	public bool IsSelected(ColourMonitorGroup group)
	{
		List<string> untickedDeviceKeys = _settings.UntickedDeviceKeys;
		if ((untickedDeviceKeys != null && untickedDeviceKeys.Contains("monitor:" + group.Identity, StringComparer.OrdinalIgnoreCase)) || (untickedDeviceKeys != null && untickedDeviceKeys.Contains("device:" + group.DeviceName, StringComparer.OrdinalIgnoreCase)) || (untickedDeviceKeys != null && untickedDeviceKeys.Contains(group.DeviceName, StringComparer.OrdinalIgnoreCase)))
		{
			return false;
		}
		if (Resolved(group) && _settings.SelectedMonitorIds != null)
		{
			return group.Identities.All((string id) => _settings.SelectedMonitorIds.Contains(id, StringComparer.OrdinalIgnoreCase));
		}
		return true;
	}

	public bool IncludesDevice(string device)
	{
		ColourMonitorGroup colourMonitorGroup = Groups.FirstOrDefault((ColourMonitorGroup g) => string.Equals(g.DeviceName, device, StringComparison.OrdinalIgnoreCase));
		if (!(colourMonitorGroup == null))
		{
			return IsSelected(colourMonitorGroup);
		}
		return true;
	}

	public string? IdentityFor(string device)
	{
		ColourMonitorGroup colourMonitorGroup = Groups.FirstOrDefault((ColourMonitorGroup g) => string.Equals(g.DeviceName, device, StringComparison.OrdinalIgnoreCase));
		if (!(colourMonitorGroup != null) || !Resolved(colourMonitorGroup))
		{
			return null;
		}
		return colourMonitorGroup.Identity;
	}

	public string KeyFor(string device)
	{
		return IdentityFor(device) ?? ("device:" + device);
	}

	public string? DeviceFor(string identity)
	{
		if (identity.StartsWith("device:", StringComparison.Ordinal))
		{
			return identity.Substring("device:".Length);
		}
		return Groups.FirstOrDefault((ColourMonitorGroup g) => Resolved(g) && string.Equals(g.Identity, identity, StringComparison.OrdinalIgnoreCase))?.DeviceName;
	}

	public void SetSelected(ColourMonitorGroup group, bool selected)
	{
		if (selected)
		{
			List<string> untickedDeviceKeys = _settings.UntickedDeviceKeys;
			if (untickedDeviceKeys != null)
			{
				untickedDeviceKeys.RemoveAll((string k) => string.Equals(k, "monitor:" + group.Identity, StringComparison.OrdinalIgnoreCase) || string.Equals(k, "device:" + group.DeviceName, StringComparison.OrdinalIgnoreCase) || string.Equals(k, group.DeviceName, StringComparison.OrdinalIgnoreCase));
				if (untickedDeviceKeys.All((string k) => k.StartsWith("inventory:", StringComparison.OrdinalIgnoreCase)))
				{
					_settings.UntickedDeviceKeys = null;
				}
			}
		}
		if (!Resolved(group))
		{
			string item = GroupKey(group);
			HashSet<string> hashSet = new HashSet<string>(_settings.UntickedDeviceKeys ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
			if (selected)
			{
				hashSet.Remove(item);
			}
			else
			{
				hashSet.Add(item);
			}
			hashSet.RemoveWhere((string k) => k.StartsWith("inventory:", StringComparison.OrdinalIgnoreCase));
			if (hashSet.Count > 0)
			{
				hashSet.Add(InventoryKey());
			}
			_settings.UntickedDeviceKeys = ((hashSet.Count == 0) ? null : hashSet.OrderBy((string k) => k, StringComparer.OrdinalIgnoreCase).ToList());
			Changed?.Invoke();
			return;
		}
		IEnumerable<string> selectedMonitorIds = _settings.SelectedMonitorIds;
		HashSet<string> hashSet2 = new HashSet<string>(selectedMonitorIds ?? Groups.Where(Resolved).SelectMany((ColourMonitorGroup g) => g.Identities), StringComparer.OrdinalIgnoreCase);
		string[] identities = group.Identities;
		foreach (string item2 in identities)
		{
			if (selected)
			{
				hashSet2.Add(item2);
			}
			else
			{
				hashSet2.Remove(item2);
			}
		}
		_settings.SelectedMonitorIds = hashSet2.OrderBy((string id) => id, StringComparer.OrdinalIgnoreCase).ToList();
		Changed?.Invoke();
	}
}
