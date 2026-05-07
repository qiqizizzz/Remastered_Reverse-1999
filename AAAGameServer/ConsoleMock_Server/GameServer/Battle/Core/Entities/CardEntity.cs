namespace GameServer.Battle.Core.Entities
{
    internal class CardEntity
    {
        private static int s_globalInstanceIdCounter = 10000;

        /// <summary>
        /// 卡牌实例Id(全局唯一)
        /// </summary>
        public int InstanceId { get; private set; }

        /// <summary>
        /// 卡牌配置Id
        /// </summary>
        public int ConfigId { get; private set; }

        /// <summary>
        /// 卡牌星级
        /// </summary>
        public int StarLevel { get; set; }

        public CardEntity(int configId)
        {
            InstanceId = System.Threading.Interlocked.Increment(ref s_globalInstanceIdCounter);
            ConfigId = configId;
            StarLevel = 1;
        }

        public CardEntity(int instanceId, int configId, int starLevel = 0)
        {
            InstanceId = instanceId;
            ConfigId = configId;
            StarLevel = starLevel;
        }

        public void ClearData()
        {
            StarLevel = 0;
        }

        #region 重写函数
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;

            if (obj is CardEntity otherCard)
            {
                return ConfigId.Equals(otherCard.ConfigId) && StarLevel == otherCard.StarLevel;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return InstanceId.GetHashCode();
        }

        public static bool operator ==(CardEntity left, CardEntity right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(CardEntity left, CardEntity right)
        {
            return !(left == right);
        }

        #endregion
    }
}
