#nullable enable
namespace Blacksmith.Minigame
{
    public enum InputResult
    {
        /// <summary>
        /// 입력 성공
        /// </summary>
        Success,

        /// <summary>
        /// 입력이 감지되지 않음
        /// </summary>
        Failure,

        /// <summary>
        /// 잘못된 입력
        /// </summary>
        Incorrect
    }
}