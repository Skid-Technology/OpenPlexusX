using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VibranceHud.Licensing;

namespace VibranceHud.Updates;

public static class UpdateVoteClient
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(6.0);

	public const int MaxNote = 500;

	private static string Url => ActivationEndpoint.Origin + "/api/app/update-vote";

	public static string BuildBody(string version, bool liked, string? note)
	{
		string text = (note ?? string.Empty).Trim();
		if (text.Length > 500)
		{
			text = text.Substring(0, 500);
		}
		return JsonSerializer.Serialize(new
		{
			version = (version ?? string.Empty),
			vote = (liked ? "up" : "down"),
			note = ((text.Length == 0) ? null : text)
		});
	}

	public static async Task SendAsync(string version, bool liked, string? note, CancellationToken cancel = default(CancellationToken))
	{
		try
		{
			using HttpClient client = new HttpClient
			{
				Timeout = Timeout
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd("PlexusX");
			using StringContent body = new StringContent(BuildBody(version, liked, note), Encoding.UTF8, "application/json");
			using (await client.PostAsync(Url, body, cancel).ConfigureAwait(continueOnCapturedContext: false))
			{
			}
		}
		catch
		{
		}
	}
}
