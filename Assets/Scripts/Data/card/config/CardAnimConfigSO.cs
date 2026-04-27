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
        public float DragScale = 1.15f;
        public float DragDuration = 0.4f;
        
        [Header("卡牌出牌相关")]
        public float PlayCommonDuration = 0.2f;
        public float PlayMoveDuration = 0.5f;
        public Vector3 PlayRotation = new Vector3(0, 0, -10f);
        public float PlayQueueScale = 0.8f;
        public int PlayRotationLoop = 2;
        
        [Header("卡牌合成相关")]
        public float CompositeCommonDuration = 0.2f;
        public float CompositeStrength = 25f;
        public int CompositeVibrato = 25;
        public float CompositeRandomMess = 45f;
    }
}