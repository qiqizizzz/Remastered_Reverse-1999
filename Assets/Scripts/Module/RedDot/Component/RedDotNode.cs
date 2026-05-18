/*
* ┌──────────────────────────────────┐
* │  描    述: 红点前缀树的节点数据模型                      
* │  类    名: RedDotNode.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;

namespace Module.RedDot.Component
{
    public class RedDotNode
    {
        public string Name { get; private set; }
        //当前红点数量(处理多级红点)
        public int Value { get; private set; }
        
        public RedDotNode Parent { get; private set; }
        public Dictionary<string, RedDotNode> Children { get; private set; }
        
        public Action<int> OnValueChanged;

        public RedDotNode(string name, RedDotNode parent = null)
        {
            Name = name;
            Value = 0;
            Parent = parent;
            Children = new Dictionary<string, RedDotNode>();
        }

        /// <summary>
        /// 改变数值并向父节点冒泡
        /// </summary>
        /// <param name="delta"></param>
        public void ChangeValue(int delta)
        {
            if(delta == 0) return;

            Value += delta;
            if(Value < 0) Value = 0;

            OnValueChanged?.Invoke(Value);
            Parent?.ChangeValue(delta);//向父节点冒泡
        }
    }
}