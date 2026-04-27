/*
* ┌───────────────────────────────────────┐
* │  描    述: 战斗卡牌数据类(记录局内状态)                      
* │  类    名: BattleCard.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────┘
*/

using Data.card;
using UnityEngine;

namespace Module.fight.Component
{
    public class BattleCardData
    {
        private readonly string InstanceId;
        public readonly CardDataSO BaseData;
        public int StarLevel;//星级

        public BattleCardData(CardDataSO baseData)
        {
            InstanceId = System.Guid.NewGuid().ToString();
            BaseData = baseData;
            StarLevel = 1;
        }

        public void ClearData()
        {
            StarLevel = 1;
        }

        #region 重写函数
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            
            if (obj is BattleCardData otherCard)
            {
                return BaseData.Equals(otherCard.BaseData) && StarLevel == otherCard.StarLevel;
            }
            return false;
        }
        
        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }

        public static bool operator ==(BattleCardData left, BattleCardData right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }
            
            return left.Equals(right);
        }
        
        public static bool operator !=(BattleCardData left, BattleCardData right)
        {
            return !(left == right);
        }

        #endregion
    }
}