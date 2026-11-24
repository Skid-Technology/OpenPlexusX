using System.Text;

namespace VibranceHud;

internal static class ShareCodeText
{
	public static string Normalize(string s)
	{
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		foreach (char c in s)
		{
			bool flag;
			switch (c)
			{
			case '\u00a0':
			case '\u200b':
			case '\u200c':
			case '\u200d':
			case '\u2060':
			case '\ufeff':
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				char value;
				switch (c)
				{
				case 'Α':
				case 'А':
					value = 'A';
					break;
				case 'Β':
				case 'В':
					value = 'B';
					break;
				case 'С':
					value = 'C';
					break;
				case 'Ε':
				case 'Е':
					value = 'E';
					break;
				case 'Η':
				case 'Н':
					value = 'H';
					break;
				case 'Κ':
				case 'К':
					value = 'K';
					break;
				case 'Μ':
				case 'М':
					value = 'M';
					break;
				case 'Ο':
				case 'О':
					value = 'O';
					break;
				case 'Ρ':
				case 'Р':
					value = 'P';
					break;
				case 'Τ':
				case 'Т':
					value = 'T';
					break;
				case 'Χ':
				case 'Х':
					value = 'X';
					break;
				case 'У':
					value = 'Y';
					break;
				case 'а':
					value = 'a';
					break;
				case 'с':
					value = 'c';
					break;
				case 'е':
					value = 'e';
					break;
				case 'о':
					value = 'o';
					break;
				case 'р':
					value = 'p';
					break;
				case 'х':
					value = 'x';
					break;
				case 'у':
					value = 'y';
					break;
				case '０':
				case '１':
				case '２':
				case '３':
				case '４':
				case '５':
				case '６':
				case '７':
				case '８':
				case '９':
				case 'Ａ':
				case 'Ｂ':
				case 'Ｃ':
				case 'Ｄ':
				case 'Ｅ':
				case 'Ｆ':
				case 'Ｇ':
				case 'Ｈ':
				case 'Ｉ':
				case 'Ｊ':
				case 'Ｋ':
				case 'Ｌ':
				case 'Ｍ':
				case 'Ｎ':
				case 'Ｏ':
				case 'Ｐ':
				case 'Ｑ':
				case 'Ｒ':
				case 'Ｓ':
				case 'Ｔ':
				case 'Ｕ':
				case 'Ｖ':
				case 'Ｗ':
				case 'Ｘ':
				case 'Ｙ':
				case 'Ｚ':
				case 'ａ':
				case 'ｂ':
				case 'ｃ':
				case 'ｄ':
				case 'ｅ':
				case 'ｆ':
				case 'ｇ':
				case 'ｈ':
				case 'ｉ':
				case 'ｊ':
				case 'ｋ':
				case 'ｌ':
				case 'ｍ':
				case 'ｎ':
				case 'ｏ':
				case 'ｐ':
				case 'ｑ':
				case 'ｒ':
				case 'ｓ':
				case 'ｔ':
				case 'ｕ':
				case 'ｖ':
				case 'ｗ':
				case 'ｘ':
				case 'ｙ':
				case 'ｚ':
					value = (char)(c - 65248);
					break;
				default:
					value = c;
					break;
				}
				stringBuilder2.Append(value);
			}
		}
		return stringBuilder.ToString();
	}
}
