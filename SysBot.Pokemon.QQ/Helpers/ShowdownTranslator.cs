using PKHeX.Core;
using System.Text.RegularExpressions;

namespace SysBot.Pokemon.QQ
{
    public class ShowdownTranslator
    {
        public static string Chinese2Showdown(string zh)
        {
            string result = "";
            var gameStrings = GameInfo.GetStrings("zh");
            var gameStringsEn = GameInfo.GetStrings("en");
            int candidateSpecieNo = 0;
            int candidateSpecieStringLength = 0;
            for (int i = 0; i < gameStrings.Species.Count; i++)
            {
                if (zh.Contains(gameStrings.Species[i]) && gameStrings.Species[i].Length > candidateSpecieStringLength)
                {
                    candidateSpecieNo = i;
                    candidateSpecieStringLength = gameStrings.Species[i].Length;
                }
            }

            if (candidateSpecieNo > 0)
            {
                result += gameStringsEn.Species[candidateSpecieNo];
                zh = zh.Replace(gameStrings.Species[candidateSpecieNo], "");
            }
            else
            {
                return result;
            }

            if (Regex.IsMatch(zh, "[A-Z?!？！]形态"))
            {
                string formsUnown = Regex.Match(zh, "([A-Z?!？！])形态").Groups?[1]?.Value ?? "";
                result += $"-{formsUnown}";
                zh = Regex.Replace(zh, "[A-Z?!？！]形态", "");
            }

            for (int i = 0; i < gameStrings.forms.Length; i++)
            {
                if (gameStrings.forms[i].Length == 0) continue;
                if (!zh.Contains(gameStrings.forms[i] + "形态")) continue;
                result += $"-{gameStringsEn.forms[i]}";
                zh = zh.Replace(gameStrings.forms[i] + "形态", "");
                break;
            }

            if (zh.Contains("公"))
            {
                result += " (M)";
                zh = zh.Replace("公", "");
            }
            else if (zh.Contains("母"))
            {
                result += " (F)";
                zh = zh.Replace("母", "");
            }

            if (Regex.IsMatch(zh, "\\d{1,3}级"))
            {
                string level = Regex.Match(zh, "(\\d{1,3})级").Groups?[1]?.Value ?? "100";
                result += $"\nLevel: {level}";
                zh = Regex.Replace(zh, "\\d{1,3}级", "");
            }

            if (zh.Contains("异色"))
            {
                result += "\nShiny: Yes";
                zh = zh.Replace("异色", "");
            }

            if (zh.Contains("头目"))
            {
                result += "\nAlpha: Yes";
                zh = zh.Replace("头目", "");
            }

            for (int i = 0; i < gameStrings.Natures.Count; i++)
            {
                if (zh.Contains(gameStrings.Natures[i]))
                {
                    result += $"\n{gameStringsEn.Natures[i]} Nature";
                    zh = zh.Replace(gameStrings.Natures[i], "");
                    break;
                }
            }

            return result;
        }
    }
}