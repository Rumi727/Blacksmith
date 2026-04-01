#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Blacksmith.Minigame
{
    [RequireComponent(typeof(RectTransform))]
    public class RhythmBar : MonoBehaviour
    {
        public RectTransform rectTransform => (RectTransform)transform;

        /// <summary>
        /// 분당 노트 (개)
        /// </summary>
        public float bpm => _bpm;
        [Header("설정")]
        [SerializeField, Tooltip("분당 노트 (개)")] float _bpm = 120;

        /// <summary>
        /// 초당 노트 속도 (px)
        /// </summary>
        public float noteSpeed => _noteSpeed;
        [SerializeField, Tooltip("초당 노트 속도 (px)")] float _noteSpeed = 375;

        /// <summary>
        /// 총 노트 이동 시간 (초)
        /// </summary>
        public float noteMoveTime => _noteMoveTime;
        [SerializeField, Tooltip("총 노트 이동 시간 (초)")] float _noteMoveTime = 4;

        /// <summary>
        /// 시작 전 딜레이 (초)
        /// </summary>
        public float startDelay => _startDelay;
        [SerializeField, Tooltip("시작 전 딜레이 (초)")] float _startDelay = 0;

        /// <summary>
        /// 노트의 총 개수 (게임을 끝낼 기준)
        /// </summary>
        public int noteCount => _noteCount;
        [SerializeField, Tooltip("노트의 총 개수 (게임을 끝낼 기준)")] int _noteCount = 15;

        /// <summary>
        /// 노트가 복제될 부모 트랜스폼을 가져옵니다
        /// </summary>
        public RectTransform noteParent => _noteParent != null ? _noteParent : rectTransform;
        [SerializeField, Tooltip("노트가 복제될 부모 트랜스폼 (정해지지 않을 시 자기 자신)")] RectTransform? _noteParent;

        /// <summary>
        /// 랜덤 복제할 노트 프리팹 목록
        /// </summary>
        public IReadOnlyList<BaseNote?> notePrefabs => _notePrefabs;
        [SerializeField, Tooltip("랜덤 복제할 노트 프리팹 목록")] List<BaseNote?> _notePrefabs = new();

        /// <summary>
        /// 판정 데이터 목록<br/>
        /// 판정 크기 기준 오름차순으로 정렬되어있어야합니다!!
        /// </summary>
        public IReadOnlyList<JudgmentData?> judgmentDatas => _judgmentDatas;
        [SerializeField, Tooltip("판정 데이터 목록\n판정 크기 기준 오름차순으로 정렬되어있어야합니다!!")]
        List<JudgmentData?> _judgmentDatas = new();

        /// <summary>
        /// 무효 판정 데이터
        /// </summary>
        public JudgmentData? missJudgmentData => _missJudgmentData;
        [SerializeField, Tooltip("무효 판정 데이터")] JudgmentData? _missJudgmentData;

        public UnityEvent<JudgmentData> onJudgment => _onJudgment;
        [Header("이벤트")]
        [SerializeField]
        [Tooltip("노트가 판정될 때 호출되는 이벤트 (매개변수: 판정 데이터)")]
        UnityEvent<JudgmentData> _onJudgment = new();

        /// <summary>
        /// 게임이 끝났을때 호출되는 이벤트입니다.<br/>
        /// 매개변수는 최종 점수 값입니다.
        /// </summary>
        public UnityEvent<int> onEnd => _onEnd;
        [SerializeField, Tooltip("게임이 끝났을때 호출되는 이벤트 (매개변수: 점수)")] UnityEvent<int> _onEnd = new();

        /// <summary>
        /// 플레이 중 여부<br/>
        /// 이 프로퍼티에 값을 할당하면 미니게임이 리셋됩니다.
        /// </summary>
        public bool isPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                Reset();
            }
        }
        bool _isPlaying;

        /// <summary>
        /// 게임 시작 후 경과 시간
        /// </summary>
        public float currentTime { get; set; } = 0;

        /// <summary>
        /// 현재 소환된 노트 오브젝트
        /// </summary>
        public IReadOnlyList<BaseNote?> currentClonedNotes => _currentClonedNotes;
        readonly List<BaseNote?> _currentClonedNotes = new();

        /// <summary>
        /// 현재 점수
        /// </summary>
        public int currentScore { get; private set; }

        /// <summary>
        /// 누적 생성된 노트 개수
        /// </summary>
        public int currentAccumulatedNotes { get; private set; }

        /// <summary>
        /// 무효 처리되지 않은 판정된 노트 개수
        /// </summary>
        public int currentProcessedNotes { get; private set; }

        void Reset()
        {
            currentTime = -startDelay;
            currentScore = 0;
            currentProcessedNotes = 0;
            currentAccumulatedNotes = 0;

            notePrefabRandomBag.Clear();
            lastNotePrefab = null;

            foreach (BaseNote? note in currentClonedNotes)
            {
                if (note == null)
                    continue;

                Destroy(note.gameObject);
            }
        }

        //void OnEnable() => isPlaying = true;

        void Update()
        {
            if (!isPlaying)
                return;

            if (notePrefabs.Count == 0 || judgmentDatas.Count == 0)
            {
                Debug.LogError("노트 프리팹 목록 또는 판정 데이터 목록이 비어있습니다!");
                return;
            }

            currentTime += Time.deltaTime;

            // 노트를 1비트 마다 복제하고, 정확한 판정을 위해 기준 시간을 복제된 노트에 전달합니다.
            float cloneTime = currentAccumulatedNotes * (60 / bpm);
            if (currentTime >= cloneTime)
                CloneNote(cloneTime);

            OnJudgmentUpdate();
            
            // 총 처리된 노트가 기준 노트보다 크면 게임 종료
            if (currentProcessedNotes >= noteCount)
            {
                isPlaying = false;
                onEnd.Invoke(currentScore);
                
                return;
            }
        }

        // 연속 4회 이상 생성되지 않게, 가방 시스템을 사용하며 리셋 시에도 중복되지 않게 마지막 노트를 기억하여 다른 노트를 선택
        readonly List<BaseNote> notePrefabRandomBag = new(9);
        BaseNote? lastNotePrefab = null;
        void ResetNotePrefabBag()
        {
            notePrefabRandomBag.Clear();

            for (int i = 0; i < notePrefabs.Count; i++)
            {
                BaseNote? item = notePrefabs[i];
                if (item == null)
                {
                    Debug.LogError($"노트 프리팹 목록의 {i}번째 요소가 null 값이거나 파괴되어 복제할 수 없습니다!");
                    continue;
                }

                int randomCount = Random.Range(1, 4);
                for (int j = 0; j < randomCount; j++)
                    notePrefabRandomBag.Add(item);
            }

            notePrefabRandomBag.Shuffle();

            //만약 섞었을 때 이전 가방의 마지막 노트가 현재 가방의 첫번째 노트와 같다면
            if (lastNotePrefab == notePrefabRandomBag[0])
            {
                for (int i = 0; i < notePrefabs.Count; i++)
                {
                    BaseNote? item = notePrefabs[i];

                    // 이전 가방의 마지막 노트가 아닌 노트중 데이터 상 첫번째 노트를 하나 삽입
                    if (item == null || lastNotePrefab == item)
                        continue;

                    int randomCount = Random.Range(1, 4);
                    for (int j = 0; j < randomCount; j++)
                        notePrefabRandomBag.Insert(0, item);
                }
            }

            lastNotePrefab = notePrefabRandomBag[^1];
        }

        void CloneNote(float cloneTime)
        {
            currentAccumulatedNotes++;

            if (notePrefabRandomBag.Count == 0)
                ResetNotePrefabBag();

            BaseNote note = Instantiate(notePrefabRandomBag[0], noteParent);
            notePrefabRandomBag.RemoveAt(0);

            note.Init(this, cloneTime);
            note.gameObject.SetActive(true);

            _currentClonedNotes.Add(note);
        }

        void OnJudgmentUpdate()
        {
            if (missJudgmentData == null)
            {
                Debug.LogError("미스 판정 데이터가 없습니다!");
                return;
            }

            // 중복 판정 방지를 위해 제일 첫번째 노트만 입력을 감지합니다.
            if (currentClonedNotes.Count <= 0)
                return;

            BaseNote? note = currentClonedNotes[0];
            if (note == null || !note.isInitialized)
                return;

            float judgmentOffset = (currentTime - noteMoveTime) - note.cloneTime.Value;
            float absJudgmentOffset = Mathf.Abs(judgmentOffset);
            InputResult inputResult = note.OnInputCheckLoop();

            switch (inputResult)
            {
                case InputResult.Success:
                {
                    Judgment(note, absJudgmentOffset);
                    break;
                }
                case InputResult.Failure:
                {
                    if (judgmentOffset <= missJudgmentData.range)
                        break;

                    Judgment(note, missJudgmentData);
                    break;
                }
                case InputResult.Incorrect:
                {
                    JudgmentData? judgmentData = judgmentDatas[^1];
                    if (judgmentData == null)
                    {
                        Debug.LogError("판정 데이터 목록의 마지막 요소가 null 값이거나 파괴되어 불러올 수 없습니다!");
                        break;
                    }

                    Judgment(note, judgmentData);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Judgment(BaseNote note, float offset)
        {
            JudgmentData? selectData = judgmentDatas.OfType<JudgmentData>().FirstOrDefault(item => offset <= item.range);
            if (selectData == null)
            {
                Debug.LogError("조건에 맞는 판정 데이터가 없습니다!");
                return;
            }

            Judgment(note, selectData);
        }

        void Judgment(BaseNote note, JudgmentData judgmentData)
        {
            if (!judgmentData.isMiss)
            {
                currentScore += judgmentData.score;
                currentProcessedNotes++;
            }

            onJudgment.Invoke(judgmentData);

            Destroy(note.gameObject);
            _currentClonedNotes.Remove(note);
        }
    }
}