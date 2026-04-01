#nullable enable
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace Blacksmith.Minigame
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class BaseNote : MonoBehaviour
    {
        public RectTransform rectTransform => (RectTransform)transform;

        /// <summary>
        /// 이 노트가 초기화되었는지 여부입니다.<br/>
        /// 참이라면 <see cref="bar"/>, <see cref="cloneTime"/> 2개의 프로퍼티가 null 값이 아님을 보장합니다. 
        /// </summary>
        [MemberNotNullWhen(true, nameof(bar), nameof(cloneTime))]
        public bool isInitialized => bar != null && cloneTime != null;

        /// <summary>
        /// 이 노트를 생성한 <see cref="RhythmBar"/>을(를) 가져옵니다.
        /// </summary>
        public RhythmBar? bar { get; private set; }

        /// <summary>
        /// 이 노트가 생성된 시간을 가져옵니다.
        /// </summary>
        public float? cloneTime { get; private set; }

        /// <summary>
        /// 노트를 초기화합니다.
        /// </summary>
        /// <param name="bar">이 노트를 생성한 오브젝트</param>
        /// <param name="cloneTime">이 노트가 생성된 기준 시간</param>
        public void Init(RhythmBar bar, float cloneTime)
        {
            this.bar = bar;
            this.cloneTime = cloneTime;

            UpdatePosition();
        }

        /// <summary>
        /// 이 노트가 판정 중일때 매 프레임 호출됩니다.
        /// </summary>
        /// <returns>참 값을 반환하면 입력된 것으로 간주합니다.</returns>
        public abstract InputResult OnInputCheckLoop();

        public virtual void Update() => UpdatePosition();

        void UpdatePosition()
        {
            if (!isInitialized)
                return;

            float x = Mathf.LerpUnclamped(bar.noteSpeed * bar.noteMoveTime, 0, (bar.currentTime - cloneTime.Value) / bar.noteMoveTime);
            float y = rectTransform.anchoredPosition.y;

            rectTransform.anchoredPosition = new Vector2(x, y);
        }
    }
}