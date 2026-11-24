namespace VibranceHud;

public sealed class LastSentEffect
{
	private float[]? _sent;

	private bool _known;

	public bool AlreadySent(float[]? next)
	{
		if (!_known)
		{
			return false;
		}
		if (_sent == null || next == null)
		{
			if (_sent == null)
			{
				return next == null;
			}
			return false;
		}
		if (_sent.Length != next.Length)
		{
			return false;
		}
		for (int i = 0; i < _sent.Length; i++)
		{
			if (_sent[i] != next[i])
			{
				return false;
			}
		}
		return true;
	}

	public void Remember(float[]? sent)
	{
		_known = true;
		_sent = ((sent == null) ? null : ((float[])sent.Clone()));
	}

	public void Forget()
	{
		_known = false;
		_sent = null;
	}
}
