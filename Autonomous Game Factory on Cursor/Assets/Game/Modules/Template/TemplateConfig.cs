using UnityEngine;

namespace Game
{
    /// <summary>
    /// 모듈 설정 데이터. 로직 없음. 에디터에서 CreateAssetMenu로 에셋 생성.
    /// 새 모듈 생성 시 &lt;ModuleName&gt;Config.cs 로 복사 후 필드만 도메인에 맞게 수정.
    /// </summary>
    [CreateAssetMenu(fileName = "TemplateConfig", menuName = "Game/Modules/TemplateConfig")]
    public class TemplateConfig : ScriptableObject
    {
        [SerializeField] float _tickInterval = 1f;

        public float TickInterval => _tickInterval;
    }
}
