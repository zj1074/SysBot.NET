using PKHeX.Core;
using System.Text.RegularExpressions;

namespace SysBot.Pokemon.Dodo
{
    public class ShowdownTranslator
    {
        public static GameStrings GameStrings = GameInfo.GetStrings("zh");
        public static GameStrings GameStringsEn = GameInfo.GetStrings("en");
        public static string Chinese2Showdown(string zh)
        {
            string result = "";
            
            int candidateSpecieNo = 0;
            int candidateSpecieStringLength = 0;
            for (int i = 0; i < GameStrings.Species.Count; i++)
            {
                if (zh.Contains(GameStrings.Species[i]) && GameStrings.Species[i].Length > candidateSpecieStringLength)
                {
                    candidateSpecieNo = i;
                    candidateSpecieStringLength = GameStrings.Species[i].Length;
                }
            }

            if (candidateSpecieNo > 0)
            {
                result += GameStringsEn.Species[candidateSpecieNo];
                zh = zh.Replace(GameStrings.Species[candidateSpecieNo], "");
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

            for (int i = 0; i < GameStrings.forms.Length; i++)
            {
                if (GameStrings.forms[i].Length == 0) continue;
                if (!zh.Contains(GameStrings.forms[i])) continue;
                result += $"-{GameStringsEn.forms[i]}";
                zh = zh.Replace(GameStrings.forms[i], "");
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

            for (int i = 0; i < GameStrings.Natures.Count; i++)
            {
                if (zh.Contains(GameStrings.Natures[i]))
                {
                    result += $"\n{GameStringsEn.Natures[i]} Nature";
                    zh = zh.Replace(GameStrings.Natures[i], "");
                    break;
                }
            }

            return result;
        }
    }
}