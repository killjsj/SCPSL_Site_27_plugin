using AutoEvent.Interfaces;

namespace AutoEvent
{
    public class RunningManTranslation : EventTranslation
    {
        public string Start { get; set; } = "<color=yellow><color=red><b><i>{name}</i></b></color>\n<i>看我派空输过来把你们一个个送上天</i>\n还有 <color=red>{time}</color> 秒开始</color>";
        public string StartPrisoners { get; set; } = "<color=yellow><color=red><b><i>{name}</i></b></color>\n<i>诚邀您参加 光州无限制格斗大赛</i>\n还有 <color=red>{time}</color> 秒开始</color>";
        public string Cycle { get; set; } = "<size=20><color=red>{name}</color>\n<color=yellow>群众 {dclasscount}</color> || <color=#14AAF5>空输: {mtfcount}</color>\n<color=red>还剩:{time}</color></size>";
        public string PrisonersWin { get; set; } = "<color=red><b><i>群众胜利</i></b></color>\n<color=red>{time}</color>";
        public string JailersWin { get; set; } = "<color=#14AAF5><b><i>空输胜利</i></b></color>\n<color=red>{time}</color>";
        public string LivesRemaining { get; set; } = "你有 {lives} 条命";
        public string NoLivesRemaining { get; set; } = "你没命了";
    }
}