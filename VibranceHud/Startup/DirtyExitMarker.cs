using System;
using System.IO;

namespace VibranceHud.Startup;

public sealed class DirtyExitMarker
{
	private readonly Func<bool> _exists;

	private readonly Action _write;

	private readonly Action _delete;

	private bool _asked;

	private bool _wasDirty;

	private bool _cleared;

	public static DirtyExitMarker Shared { get; set; } = Inert();

	public DirtyExitMarker(Func<bool> exists, Action write, Action delete)
	{
		_exists = exists;
		_write = write;
		_delete = delete;
	}

	public bool ClaimPreviousRunWasDirty()
	{
		if (_asked)
		{
			return _wasDirty;
		}
		_asked = true;
		try
		{
			_wasDirty = _exists();
		}
		catch
		{
			_wasDirty = false;
		}
		try
		{
			_write();
		}
		catch
		{
		}
		return _wasDirty;
	}

	public void Clear()
	{
		if (!_asked || _cleared)
		{
			return;
		}
		_cleared = true;
		try
		{
			_delete();
		}
		catch
		{
		}
	}

	public static DirtyExitMarker Inert()
	{
		return new DirtyExitMarker(() => false, () =>
		{
		}, () =>
		{
		});
	}

	public static DirtyExitMarker InLocalAppData()
	{
		string path = Path.Combine(AppFolders.Local, "running.marker");
		return new DirtyExitMarker(() => File.Exists(path), () =>
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, DateTime.UtcNow.ToString("O"));
		}, () =>
		{
			File.Delete(path);
		});
	}
}
