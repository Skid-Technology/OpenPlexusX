using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace VibranceHud.Remote;

internal sealed class PhoneControl : IDisposable
{
	private const int FlushIntervalMs = 50;

	private const int GestureIdleMs = 250;

	private readonly VibranceEngine _engine;

	private readonly Func<IReadOnlyList<string>> _presetNames;

	private readonly Action<int> _applyPreset;

	private readonly Action _changed;

	private readonly object _gate = new object();

	private readonly Dictionary<string, int> _pending = new Dictionary<string, int>(StringComparer.Ordinal);

	private readonly Timer _timer;

	private DateTime _lastSample = DateTime.MinValue;

	private bool _gestureOpen;

	internal PhoneControl(VibranceEngine engine, Func<IReadOnlyList<string>> presetNames, Action<int> applyPreset, Action changed)
	{
		_engine = engine;
		_presetNames = presetNames;
		_applyPreset = applyPreset;
		_changed = changed;
		_timer = new Timer
		{
			Interval = 50
		};
		_timer.Tick += (object? s, EventArgs e) =>
		{
			Flush();
		};
		_timer.Start();
	}

	internal void Queue(string field, int value)
	{
		lock (_gate)
		{
			_pending[field] = value;
			_lastSample = DateTime.UtcNow;
		}
	}

	internal string StateJson()
	{
		List<string> list = new List<string>();
		try
		{
			list.AddRange(_presetNames());
		}
		catch
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{\"controls\":[");
		for (int i = 0; i < PhoneFields.Sliders.Length; i++)
		{
			PhoneField phoneField = PhoneFields.Sliders[i];
			if (i > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append("{\"key\":\"").Append(phoneField.Key).Append("\",\"label\":\"")
				.Append(Escape(phoneField.Label))
				.Append("\",\"min\":")
				.Append(phoneField.Min)
				.Append(",\"max\":")
				.Append(phoneField.Max)
				.Append(",\"neutral\":")
				.Append(phoneField.Neutral)
				.Append(",\"suffix\":\"")
				.Append(Escape(phoneField.Suffix))
				.Append("\",\"value\":")
				.Append(Read(phoneField.Key))
				.Append('}');
		}
		stringBuilder.Append("],\"comparing\":").Append(_engine.IsPreviewingNeutral ? "true" : "false").Append(",\"presets\":[");
		for (int j = 0; j < list.Count; j++)
		{
			if (j > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append('"').Append(Escape(list[j])).Append('"');
		}
		stringBuilder.Append("]}");
		return stringBuilder.ToString();
	}

	private int Read(string key)
	{
		return key switch
		{
			"saturation" => _engine.Saturation, 
			"vibrance" => _engine.Vibrance, 
			"brightness" => _engine.Brightness, 
			"gamma" => _engine.Gamma, 
			"contrast" => _engine.Contrast, 
			"temperature" => _engine.Temperature, 
			"highlights" => _engine.Tone.Highlights, 
			"shadows" => _engine.Tone.Shadows, 
			_ => 0, 
		};
	}

	private static string Escape(string text)
	{
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		foreach (char c in text)
		{
			if (c == '"' || c == '\\')
			{
				stringBuilder.Append('\\').Append(c);
			}
			else if (c < ' ')
			{
				stringBuilder.Append(' ');
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	internal void PumpForTest()
	{
		Flush();
	}

	private void Flush()
	{
		bool flag;
		KeyValuePair<string, int>[] array;
		lock (_gate)
		{
			if (_pending.Count == 0)
			{
				flag = _gestureOpen && (DateTime.UtcNow - _lastSample).TotalMilliseconds > 250.0;
				if (!flag)
				{
					return;
				}
				array = Array.Empty<KeyValuePair<string, int>>();
			}
			else
			{
				array = new KeyValuePair<string, int>[_pending.Count];
				((ICollection<KeyValuePair<string, int>>)_pending).CopyTo(array, 0);
				_pending.Clear();
				flag = false;
			}
		}
		try
		{
			KeyValuePair<string, int>[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				KeyValuePair<string, int> keyValuePair = array2[i];
				if (keyValuePair.Key == "preset")
				{
					CloseGesture();
					_applyPreset(keyValuePair.Value);
					continue;
				}
				if (keyValuePair.Key == "compare")
				{
					CloseGesture();
					_engine.PreviewNeutral(keyValuePair.Value != 0);
					continue;
				}
				if (!_gestureOpen)
				{
					_engine.BeginDrag();
					_gestureOpen = true;
				}
				if (_engine.IsPreviewingNeutral)
				{
					_engine.PreviewNeutral(on: false);
				}
				switch (keyValuePair.Key)
				{
				case "saturation":
					_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Saturation;
					_engine.Saturation = keyValuePair.Value;
					break;
				case "vibrance":
					_engine.DriverKnobFollows = DisplayOnlyLook.Knob.Vibrance;
					_engine.Vibrance = keyValuePair.Value;
					break;
				case "brightness":
					_engine.Brightness = keyValuePair.Value;
					break;
				case "gamma":
					_engine.Gamma = keyValuePair.Value;
					break;
				case "contrast":
					_engine.Contrast = keyValuePair.Value;
					break;
				case "temperature":
					_engine.Temperature = keyValuePair.Value;
					break;
				case "highlights":
					_engine.Tone = _engine.Tone with
					{
						Highlights = keyValuePair.Value
					};
					break;
				case "shadows":
					_engine.Tone = _engine.Tone with
					{
						Shadows = keyValuePair.Value
					};
					break;
				}
			}
			if (flag)
			{
				CloseGesture();
			}
			if ((array.Length != 0) | flag)
			{
				_changed();
			}
		}
		catch
		{
			CloseGesture();
		}
	}

	private void CloseGesture()
	{
		if (_gestureOpen)
		{
			_gestureOpen = false;
			_engine.EndDrag();
		}
	}

	public void Dispose()
	{
		_timer.Stop();
		_timer.Dispose();
		CloseGesture();
	}
}
