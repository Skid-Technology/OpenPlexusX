namespace VibranceHud;

public interface ISaturationOverlay
{
	bool LastApplySucceeded => true;

	void Apply(float[] matrix);

	void Clear();

	void Forget()
	{
	}
}
