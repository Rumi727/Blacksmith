#nullable enable
using UnityEngine;

namespace Blacksmith.Minigame
{
    [CreateAssetMenu(fileName = "Judgment Data", menuName = "Scriptable Objects/Minigame/Judgment Data")]
    public sealed class JudgmentData : ScriptableObject
    {
        /// <summary>
        /// 판정 ID
        /// </summary>
        public string? id;

        /// <summary>
        /// 판정 범위 (단위: 초)<br/>
        /// 이 값보다 오차가 작거나 같다면 범위에 들어온 것으로 판정합니다.
        /// </summary>
        public float range;

        /// <summary>
        /// 판정이 가지는 점수
        /// </summary>
        public int score;

        /// <summary>
        /// 무효 판정 여부
        /// </summary>
        public bool isMiss;
    }
}