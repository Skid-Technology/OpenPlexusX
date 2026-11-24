using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace VibranceHud;

public sealed class DxOverlay : ISaturationOverlay, IDisplayOverlay, IDisposable
{
	private sealed class ActiveOutput
	{
		public DxDevice.OutputTarget Target;

		public DxShader Shader;

		public DxCapture Capture;
	}

	private static readonly float[] Identity = new float[25]
	{
		1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f,
		0f, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f,
		0f, 0f, 0f, 0f, 1f
	};

	private const PresentFlags AlphaPremultiplied = PresentFlags.None;

	private const int ActiveSleepMs = 16;

	private const int IdleSleepMs = 250;

	private readonly DxDevice _device;

	private readonly List<ActiveOutput> _active;

	private readonly CancellationTokenSource _cts;

	private readonly Task _renderLoop;

	private readonly object _matrixLock = new object();

	private float[] _currentMatrix;

	private volatile bool _isRendering;

	public OverlayMode ActiveMode => OverlayMode.Dx;

	public DxInitFailureKind LastFailure { get; private set; }

	public string LastFailureMessage { get; private set; } = "";

	public int LastFailureCode { get; private set; }

	public bool IsAvailable { get; }

	public bool EffectIsLive => IsAvailable;

	public bool IsRendering => _isRendering;

	public string WhyNothingIsDrawn
	{
		get
		{
			if (!IsAvailable)
			{
				if (!string.IsNullOrEmpty(LastFailureMessage))
				{
					return LastFailureMessage;
				}
				return "DirectX 11 did not start on this PC";
			}
			return "";
		}
	}

	public DxOverlay()
	{
		_active = new List<ActiveOutput>();
		_currentMatrix = Identity;
		_device = new DxDevice();
		if (!_device.IsAvailable)
		{
			IsAvailable = false;
			LastFailure = _device.LastFailure;
			LastFailureMessage = _device.LastFailureMessage;
			LastFailureCode = _device.LastFailureCode;
			_device.Dispose();
			_device = null;
			return;
		}
		Exception firstFailure = null;
		TolerantOutputBuilder.Build(_device.Targets.Count, (int i) =>
		{
			DxDevice.OutputTarget outputTarget = _device.Targets[i];
			DxShader dxShader = null;
			try
			{
				dxShader = new DxShader(outputTarget.Device, outputTarget.Device.ImmediateContext);
				DxCapture capture = new DxCapture(outputTarget.Device, outputTarget.Output);
				_active.Add(new ActiveOutput
				{
					Target = outputTarget,
					Shader = dxShader,
					Capture = capture
				});
			}
			catch
			{
				dxShader?.Dispose();
				throw;
			}
		}, (int i, Exception ex) =>
		{
			if (firstFailure == null)
			{
				firstFailure = ex;
			}
		});
		if (_active.Count == 0)
		{
			(DxInitFailureKind Kind, string Short, string Hint) tuple = DxInitFailureMapper.MapGeneric(firstFailure ?? new InvalidOperationException("No renderable display output"));
			DxInitFailureKind item = tuple.Kind;
			string item2 = tuple.Short;
			LastFailure = item;
			LastFailureMessage = item2;
			_device.Dispose();
			_device = null;
			IsAvailable = false;
		}
		else
		{
			_cts = new CancellationTokenSource();
			_renderLoop = Task.Run(() =>
			{
				RenderLoop(_cts.Token);
			});
			IsAvailable = true;
		}
	}

	public void Apply(float[] matrix)
	{
		if (!IsAvailable)
		{
			SayNothingIsDrawn();
			return;
		}
		lock (_matrixLock)
		{
			_currentMatrix = (float[])matrix.Clone();
		}
	}

	public void Clear()
	{
		if (!IsAvailable)
		{
			SayNothingIsDrawn();
		}
		else
		{
			Apply(Identity);
		}
	}

	private void SayNothingIsDrawn()
	{
		ColourTrace.Refused("SCREEN EFFECT", WhyNothingIsDrawn);
	}

	public void Dispose()
	{
		if (!IsAvailable)
		{
			return;
		}
		_cts.Cancel();
		try
		{
			_renderLoop.Wait(TimeSpan.FromSeconds(2.0));
		}
		catch
		{
		}
		foreach (ActiveOutput item in _active)
		{
			item.Capture.Dispose();
			item.Shader.Dispose();
		}
		_active.Clear();
		_device.Dispose();
	}

	private void RenderLoop(CancellationToken ct)
	{
		RawColor4 colorRGBA = new RawColor4(0f, 0f, 0f, 0f);
		bool flag = false;
		while (!ct.IsCancellationRequested)
		{
			float[] currentMatrix;
			lock (_matrixLock)
			{
				currentMatrix = _currentMatrix;
			}
			if (IsIdentityMatrix(currentMatrix))
			{
				if (!flag)
				{
					foreach (DxDevice.OutputTarget target2 in _device.Targets)
					{
						DeviceContext immediateContext = target2.Device.ImmediateContext;
						immediateContext.OutputMerger.SetRenderTargets(target2.Rtv);
						immediateContext.ClearRenderTargetView(target2.Rtv, colorRGBA);
						target2.SwapChain.Present(1, PresentFlags.None);
					}
					flag = true;
					_isRendering = false;
				}
				Thread.Sleep(250);
				continue;
			}
			flag = false;
			_isRendering = true;
			foreach (ActiveOutput item in _active)
			{
				DxDevice.OutputTarget target = item.Target;
				DxCapture capture = item.Capture;
				DxShader shader = item.Shader;
				DeviceContext immediateContext2 = target.Device.ImmediateContext;
				if (!shader.ApplyMatrix(currentMatrix))
				{
					immediateContext2.OutputMerger.SetRenderTargets(target.Rtv);
					immediateContext2.ClearRenderTargetView(target.Rtv, colorRGBA);
					target.SwapChain.Present(1, PresentFlags.None);
					_isRendering = false;
				}
				else
				{
					capture.TryCapture();
					immediateContext2.OutputMerger.SetRenderTargets(target.Rtv);
					immediateContext2.ClearRenderTargetView(target.Rtv, colorRGBA);
					immediateContext2.Rasterizer.SetViewport(0f, 0f, target.Width, target.Height);
					shader.Bind(capture.FrameView);
					shader.Draw();
					target.SwapChain.Present(1, PresentFlags.None);
				}
			}
			Thread.Sleep(16);
		}
	}

	private static bool IsIdentityMatrix(float[] m)
	{
		if (m == Identity)
		{
			return true;
		}
		for (int i = 0; i < 25; i++)
		{
			if (Math.Abs(m[i] - Identity[i]) > 0.0001f)
			{
				return false;
			}
		}
		return true;
	}
}
