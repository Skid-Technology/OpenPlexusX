using System.Collections.Generic;

namespace VibranceHud;

public readonly record struct LookNote(string Text, bool Refused)
{
	public static LookNote None => new LookNote("", Refused: false);

	public void AddTo(List<string> applied, List<string> notes)
	{
		if (!Refused)
		{
			applied.Add("colour");
		}
		if (!string.IsNullOrEmpty(Text))
		{
			notes.Add(Text);
		}
	}
}
