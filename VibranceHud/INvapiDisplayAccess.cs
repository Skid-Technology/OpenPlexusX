namespace VibranceHud;

internal interface INvapiDisplayAccess
{
	int Resolve();

	int ReadCurrentLevel(int display);

	int ReadDefaultLevel(int display);

	void WriteLevel(int display, int level);

	string DisplayId(int display)
	{
		return "display-" + display;
	}
}
