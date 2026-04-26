/*
* ┌──────────────────────────────────┐
* │  描    述: 英雄实体 
* │  类    名: HeroEntity.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Data.card;
using UnityEngine;

namespace Module.Character
{
    public class HeroEntity : BaseCharacter
    {
        //行动点相关
        public int ActionPoint { get; private set; }
        public const int MaxActionPoint = 5;

        public void AddActionPoint(int value = 1)
        {
            ActionPoint = Mathf.Min(MaxActionPoint, ActionPoint + value);
        }

        public void SetActionPoint(int value)
        {
            ActionPoint = Mathf.Clamp(value, 0, MaxActionPoint);
        }
    }
}