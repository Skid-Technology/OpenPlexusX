using System.Threading.Tasks;

namespace VibranceHud;

public interface IStartupRegistration
{
	StartupState State { get; }

	Task<StartupState> RefreshAsync();

	Task<StartupState> TurnOnAsync();

	void TurnOff();
}
