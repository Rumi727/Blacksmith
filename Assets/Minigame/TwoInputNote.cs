#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Blacksmith.Minigame
{
    public sealed class TwoInputNote : BaseNote
    {
        /// <summary>
        /// 감지할 키보드 키 코드
        /// </summary>
        public List<KeyCode> keyCodes => _keyCodes;
        [SerializeField] List<KeyCode> _keyCodes = new();

        float timer = 0;
        readonly HashSet<KeyCode> downedKeyCode = new();

        public override InputResult OnInputCheckLoop()
        {
            for (int i = 0; i < keyCodes.Count; i++)
            {
                KeyCode keyCode = keyCodes[i];
                if (Input.GetKeyDown(keyCode))
                    downedKeyCode.Add(keyCode);
            }

            if (downedKeyCode.Count >= keyCodes.Count)
                return InputResult.Success;

            if (downedKeyCode.Count > 0)
                timer += Time.deltaTime;

            if (timer >= 0.1)
                return InputResult.Incorrect;

            return InputResult.Failure;
        }
    }
}