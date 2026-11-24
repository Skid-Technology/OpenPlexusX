using System;
using System.Text;
using System.Text.Json;

namespace VibranceHud.Discord;

public static class DiscordRpc
{
	public const string ApplicationId = "1537861543633494146";

	public const int OpHandshake = 0;

	public const int OpFrame = 1;

	public static byte[] Frame(int opcode, string json)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		byte[] array = new byte[8 + bytes.Length];
		BitConverter.GetBytes(opcode).CopyTo(array, 0);
		BitConverter.GetBytes(bytes.Length).CopyTo(array, 4);
		bytes.CopyTo(array, 8);
		return array;
	}

	public static string HandshakeJson()
	{
		return JsonSerializer.Serialize(new
		{
			v = 1,
			client_id = "1537861543633494146"
		});
	}

	public static string SetActivityJson(int pid, string? presetName)
	{
		object assets = new
		{
			large_image = "logo",
			large_text = "PlexusX"
		};
		object[] buttons = new object[2]
		{
			new
			{
				label = "View Site",
				url = "https://plexusx.lol"
			},
			new
			{
				label = "Download",
				url = "https://plexusx.lol"
			}
		};
		object activity = ((presetName == null) ? ((object)new
		{
			details = "Colour, all the way up",
			assets = assets,
			buttons = buttons
		}) : ((object)new
		{
			details = "Colour, all the way up",
			state = presetName,
			assets = assets,
			buttons = buttons
		}));
		return JsonSerializer.Serialize(new
		{
			cmd = "SET_ACTIVITY",
			args = new { pid, activity },
			nonce = Guid.NewGuid().ToString("N")
		});
	}
}
