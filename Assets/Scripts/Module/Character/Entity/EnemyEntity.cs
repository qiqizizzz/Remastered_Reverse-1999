/*
* ┌──────────────────────────────────┐
* │  描    述: 敌人实体类                      
* │  类    名: EnemyEntity.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/


using System;
using Common.Defines;
using Data.card;
using UnityEngine;

namespace Module.Character
{
    public class EnemyEntity : BaseCharacter
    {
        private void OnMouseDown()
        {
            if(CurrentStateType == CharacterStateType.Die) return;
            
            Debug.Log($"点击了敌人：{_characterData.Name}");
            
            //TODO:通知更新UI并传递InstanceID
            GameApp.MessageCenter.PostEvent(EventDefines.OnSelectEnemyTarget, InstanceID);
        }
    }
}