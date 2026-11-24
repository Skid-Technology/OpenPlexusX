using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace VibranceHud;

internal static class DownloadHelper
{
	internal static TimeSpan StallLimit { get; set; } = TimeSpan.FromSeconds(60.0);

	internal static TimeSpan HeadersLimit { get; set; } = TimeSpan.FromSeconds(30.0);

	public static async Task<string?> DownloadToTempAsync(Uri url, string partialPath, IProgress<int>? progress)
	{
		_ = 4;
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(partialPath)));
			long have = (File.Exists(partialPath) ? new FileInfo(partialPath).Length : 0);
			using HttpClient client = new HttpClient
			{
				Timeout = Timeout.InfiniteTimeSpan
			};
			client.DefaultRequestHeaders.UserAgent.ParseAdd(UpdateService.UpdaterUserAgent);
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
			if (have > 0)
			{
				request.Headers.Range = new RangeHeaderValue(have, null);
			}
			HttpResponseMessage httpResponseMessage;
			using (CancellationTokenSource headers = new CancellationTokenSource(HeadersLimit))
			{
				try
				{
					httpResponseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, headers.Token);
				}
				catch (OperationCanceledException) when (headers.IsCancellationRequested)
				{
					UpdateService.LastDownloadError = $"The download server did not answer within {HeadersLimit.TotalSeconds:0} s.";
					return null;
				}
			}
			using (httpResponseMessage)
			{
				if (httpResponseMessage.StatusCode == HttpStatusCode.RequestedRangeNotSatisfiable && have > 0)
				{
					progress?.Report(100);
					return partialPath;
				}
				httpResponseMessage.EnsureSuccessStatusCode();
				bool resume = false;
				long total;
				if (httpResponseMessage.StatusCode == HttpStatusCode.PartialContent)
				{
					ContentRangeHeaderValue contentRange = httpResponseMessage.Content.Headers.ContentRange;
					if (have == 0L || contentRange == null || contentRange.From != have)
					{
						UpdateService.LastDownloadError = $"The download server sent bytes {contentRange?.From}- when asked for {have}-.";
						UpdateService.SafeDelete(partialPath);
						return null;
					}
					resume = true;
					total = contentRange.Length ?? (-1);
				}
				else
				{
					total = httpResponseMessage.Content.Headers.ContentLength ?? (-1);
					have = 0L;
				}
				using Stream source = await httpResponseMessage.Content.ReadAsStreamAsync();
				using FileStream target = new FileStream(partialPath, resume ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
				using CancellationTokenSource headers = new CancellationTokenSource();
				byte[] buffer = new byte[81920];
				long read = 0L;
				while (true)
				{
					headers.CancelAfter(StallLimit);
					int n;
					try
					{
						n = await source.ReadAsync(buffer, headers.Token);
					}
					catch (OperationCanceledException) when (headers.IsCancellationRequested)
					{
						UpdateService.LastDownloadError = $"The download stalled: no data for {StallLimit.TotalSeconds:0} s after {have + read:N0} bytes.";
						return null;
					}
					if (n <= 0)
					{
						break;
					}
					await target.WriteAsync(buffer.AsMemory(0, n));
					await target.FlushAsync();
					read += n;
					if (total > 0)
					{
						progress?.Report((int)Math.Min(100L, (have + read) * 100 / total));
					}
				}
				return partialPath;
			}
		}
		catch (Exception ex3)
		{
			UpdateService.LastDownloadError = "Download failed: " + ex3.Message;
			return null;
		}
	}
}
