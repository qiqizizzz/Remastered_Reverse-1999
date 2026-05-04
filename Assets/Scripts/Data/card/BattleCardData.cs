/*
* ┌───────────────────────────────────────┐
* │  描    述: 战斗卡牌数据类(记录局内状态)                      
* │  类    名: BattleCard.cs       
* │  创    建: By qiqizizzz
* └───────────────────────────────────────┘
*/

namespace Data.card
{
    public class BattleCardData
    {
        private readonly string InstanceId;
        public readonly int ConfigId;
        public int StarLevel;//星级

        public BattleCardData(int configId)
        {
            InstanceId = System.Guid.NewGuid().ToString();
            ConfigId = configId;
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
                return ConfigId.Equals(otherCard.ConfigId) && StarLevel == otherCard.StarLevel;
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