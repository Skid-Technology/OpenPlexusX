using System;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;

namespace VibranceHud;

internal sealed class DxCapture : IDisposable
{
	private readonly OutputDuplication _duplication;

	private readonly Texture2D _frameTex;

	private readonly ShaderResourceView _srv;

	private readonly SharpDX.Direct3D11.Device _device;

	public Texture2D Frame => _frameTex;

	public ShaderResourceView FrameView => _srv;

	public DxCapture(SharpDX.Direct3D11.Device device, Output1 output)
	{
		_device = device;
		_duplication = output.DuplicateOutput(device);
		RawRectangle desktopBounds = output.Description.DesktopBounds;
		int width = desktopBounds.Right - desktopBounds.Left;
		int height = desktopBounds.Bottom - desktopBounds.Top;
		_frameTex = new Texture2D(device, new Texture2DDescription
		{
			Width = width,
			Height = height,
			MipLevels = 1,
			ArraySize = 1,
			Format = Format.B8G8R8A8_UNorm,
			SampleDescription = new SampleDescription(1, 0),
			Usage = ResourceUsage.Default,
			CpuAccessFlags = CpuAccessFlags.None,
			BindFlags = BindFlags.ShaderResource,
			OptionFlags = ResourceOptionFlags.None
		});
		_srv = new ShaderResourceView(device, _frameTex);
	}

	public bool TryCapture()
	{
		SharpDX.DXGI.Resource desktopResourceOut = null;
		try
		{
			if (_duplication.TryAcquireNextFrame(0, out var frameInfoRef, out desktopResourceOut).Failure)
			{
				return false;
			}
			if (frameInfoRef.LastPresentTime == 0L)
			{
				return false;
			}
			using (Texture2D source = desktopResourceOut.QueryInterface<Texture2D>())
			{
				_device.ImmediateContext.CopyResource(source, _frameTex);
			}
			return true;
		}
		catch (SharpDXException)
		{
			return false;
		}
		finally
		{
			desktopResourceOut?.Dispose();
			try
			{
				_duplication.ReleaseFrame();
			}
			catch
			{
			}
		}
	}

	public void Dispose()
	{
		_srv.Dispose();
		_frameTex.Dispose();
		_duplication.Dispose();
	}
}
