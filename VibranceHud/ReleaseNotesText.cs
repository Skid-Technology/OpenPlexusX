using System;
using System.Text.RegularExpressions;

namespace VibranceHud;

public static class ReleaseNotesText
{
	public static string ToPlainText(string? notes)
	{
		if (string.IsNullOrWhiteSpace(notes))
		{
			return "";
		}
		return Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(Regex.Replace(notes.Replace("\r\n", "\n").Replace('\r', '\n'), "^\\s{0,3}#{1,6}\\s*", "", RegexOptions.Multiline), "^(\\s*)[-*+]\\s+", "$1• ", RegexOptions.Multiline), "\\*\\*(.+?)\\*\\*", "$1"), "__(.+?)__", "$1"), "(?<!\\*)\\*(?!\\s)(.+?)(?<!\\s)\\*(?!\\*)", "$1"), "```[a-zA-Z0-9]*\\n?", "").Replace("`", ""), "\\[([^\\]]+)\\]\\([^)]+\\)", "$1"), "^\\s*([-*_]\\s*){3,}$", "", RegexOptions.Multiline), "[ \\t]+$", "", RegexOptions.Multiline), "\\n{3,}", "\n\n").Trim().Replace("\n", Environment.NewLine);
	}
}
