#nullable enable
using UnityEngine;

namespace Blacksmith.Minigame
{
    public sealed class NormalNote : BaseNote
    {
        /// <summary>
        /// 감지할 키보드 키 코드
        /// </summary>
        public KeyCode keyCode => _keyCode;
        [SerializeField, Tooltip("감지할 키보드 키 코드")] KeyCode _keyCode;

        /// <summary>
        /// 감지할 키보드 잘못된 키 코드
        /// </summary>
        public KeyCode incorrectKeyCode => _incorrectKeyCode;
        [SerializeField, Tooltip("감지할 키보드 잘못된 키 코드")] KeyCode _incorrectKeyCode;

        public override InputResult OnInputCheckLoop()
        {
            if (Input.GetKeyDown(keyCode))
                return InputResult.Success;
            else if (Input.GetKeyDown(incorrectKeyCode))
                return InputResult.Incorrect;

            return InputResult.Failure;
        }
    }
}