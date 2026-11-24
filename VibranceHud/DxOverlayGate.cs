using System;

namespace VibranceHud;

public sealed class DxOverlayGate
{
	private readonly Func<bool> _qualityColour;

	private readonly Func<bool> _renderPathVerified;

	public static bool RenderPathVerified => false;

	public bool IsOpen
	{
		get
		{
			try
			{
				return _qualityColour() && _renderPathVerified();
			}
			catch
			{
				return false;
			}
		}
	}

	public DxOverlayGate(Func<bool> qualityColour)
		: this(qualityColour, () => RenderPathVerified)
	{
	}

	internal DxOverlayGate(Func<bool> qualityColour, Func<bool> renderPathVerified)
	{
		_qualityColour = qualityColour ?? ((Func<bool>)(() => false));
		_renderPathVerified = renderPathVerified ?? ((Func<bool>)(() => RenderPathVerified));
	}
}
