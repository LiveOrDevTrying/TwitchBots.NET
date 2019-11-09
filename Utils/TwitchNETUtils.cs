using System.Collections.Generic;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Models.Extensions.NetCore;

namespace Twitch.NET.Utils
{
    public static class TwitchNETUtils
    {
        public static ChatColorPresets GetChatColor(string hexCode)
        {
            var validcolors = new KeyValuePair<string, ChatColorPresets>[]
            {
                new KeyValuePair<string, ChatColorPresets>("0000FF", ChatColorPresets.Blue),
                new KeyValuePair<string, ChatColorPresets>("FF7F50", ChatColorPresets.Coral),
                new KeyValuePair<string, ChatColorPresets>("1E90FF", ChatColorPresets.DodgerBlue),
                new KeyValuePair<string, ChatColorPresets>("00FF7F", ChatColorPresets.SpringGreen),
                new KeyValuePair<string, ChatColorPresets>("9ACD32", ChatColorPresets.YellowGreen),
                new KeyValuePair<string, ChatColorPresets>("008000", ChatColorPresets.Green),
                new KeyValuePair<string, ChatColorPresets>("FF4500", ChatColorPresets.OrangeRed),
                new KeyValuePair<string, ChatColorPresets>("FF0000", ChatColorPresets.Red),
                new KeyValuePair<string, ChatColorPresets>("DAA520", ChatColorPresets.GoldenRod),
                new KeyValuePair<string, ChatColorPresets>("FF69B4", ChatColorPresets.HotPink),
                new KeyValuePair<string, ChatColorPresets>("5F9EA0", ChatColorPresets.CadetBlue),
                new KeyValuePair<string, ChatColorPresets>("2E8B57", ChatColorPresets.SeaGreen),
                new KeyValuePair<string, ChatColorPresets>("D2691E", ChatColorPresets.Chocolate),
                new KeyValuePair<string, ChatColorPresets>("8A2BE2", ChatColorPresets.BlueViolet),
                new KeyValuePair<string, ChatColorPresets>("B22222", ChatColorPresets.Firebrick)
            };

            var c = ColorTranslator.FromHtml("#" + hexCode);
            var diff = 200000;
            var closestColor = ChatColorPresets.Blue;

            foreach (var validColor in validcolors)
            {
                var color = ColorTranslator.FromHtml("#" + validColor.Key);
                var currentDiff = (c.R - color.R) * (c.R - color.R) + (c.G - color.G) * (c.G - color.G) + (c.B - color.B) * (c.B - color.B);

                if (diff > currentDiff)
                {
                    closestColor = validColor.Value;
                    diff = currentDiff;
                }
            }

            return closestColor;
        }
        public static string GetHexCode(ChatColorPresets chatColor)
        {
            switch (chatColor)
            {
                case ChatColorPresets.Blue:
                    return "0000FF";
                case ChatColorPresets.Coral:
                    return "FF7F50";
                case ChatColorPresets.DodgerBlue:
                    return "1E90FF";
                case ChatColorPresets.SpringGreen:
                    return "00FF7F";
                case ChatColorPresets.YellowGreen:
                    return "9ACD32";
                case ChatColorPresets.Green:
                    return "008000";
                case ChatColorPresets.OrangeRed:
                    return "FF4500";
                case ChatColorPresets.Red:
                    return "FF0000";
                case ChatColorPresets.GoldenRod:
                    return "DAA520";
                case ChatColorPresets.HotPink:
                    return "FF69B4";
                case ChatColorPresets.CadetBlue:
                    return "5F9EA0";
                case ChatColorPresets.SeaGreen:
                    return "2E8B57";
                case ChatColorPresets.Chocolate:
                    return "D2691E";
                case ChatColorPresets.BlueViolet:
                    return "8A2BE2";
                case ChatColorPresets.Firebrick:
                    return "B22222";
                default:
                    return string.Empty;
            }
        }
    }
}
