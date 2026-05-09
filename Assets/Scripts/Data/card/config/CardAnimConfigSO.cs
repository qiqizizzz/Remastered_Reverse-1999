/*
* ┌──────────────────────────────────┐
* │  描    述: 卡牌动画参数配置                      
* │  类    名: CardAnimConfigSO.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Data.card
{
    [CreateAssetMenu(fileName = "CardAnimConfig", menuName = "数据配置/Config/CardAnimConfig")]
    public class CardAnimConfigSO : ScriptableObject
    {
        [Header("卡牌移动相关")]
        public float CardWidth = 180f;
        public float CardSpacing = 10f;
        public float StartX = 90f;
        public float MoveDuration = 0.4f;
        public Vector2 SpawnPos = new Vector2(-2200f, 0);
        
        [Header("卡牌拖拽相关")]
        public float DragScale = 1f;
        public float DragDuration = 0.4f;
        
        [Header("卡牌出牌相关")]
        public float PlayCommonDuration = 0.2f;
        public float PlayMoveDuration = 0.5f;
        public Vector3 PlayRotation = new Vector3(0, 0, -10f);
        public float PlayQueueScale = 0.8f;
        public int PlayRotationLoop = 2;
        
        [Header("卡牌合成相关")]
        public float CompositeMoveDuration = 0.25f;//飞向中心
        public float CompositeScaleUpDuration = 0.12f;//膨胀
        public float CompositeScaleDownDuration = 0.1f;//收缩
        public float CompositeShakeDuration = 0.3f;//抖动时长
        public float CompositeSettleDuration = 0.2f;//回弹
        public float CompositeStrength = 30f;//抖动强度
        public int CompositeVibrato = 30;//抖动频率
        public float CompositeRandomMess = 60f;//抖动随机度
    }
}