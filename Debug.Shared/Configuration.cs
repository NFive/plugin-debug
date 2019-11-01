using NFive.SDK.Core.Controllers;
using NFive.SDK.Core.Input;

namespace NFive.Debug.Shared
{
	public class Configuration : ControllerConfiguration
	{
		public InputControl ActivateKey { get; set; } = InputControl.ReplayStartStopRecordingSecondary; // Default to F2
	}
}
