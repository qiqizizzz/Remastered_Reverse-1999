/*
* ┌──────────────────────────────────┐
* │  描    述: 准备战斗界面                      
* │  类    名: PrepareFightView.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Common.Defines;
using Data.card;
using Data.level;
using Module.fight.Network;
using Module.level;
using Module.level.Component;
using Module.Loading;
using Module.Matchmaking;
using MVC;
using MVC.Extensions;
using MVC.View;
using TMPro;
using Common;
using UnityEngine;
using UnityEngine.UI;

namespace Module.View
{
    public class PrepareFightView : BaseView
    {
        [Header("UI组件")]
        private TextMeshProUGUI levelTargetText1;
        private TextMeshProUGUI levelTargetText2;
        private Transform selectFormationArea;
        
        [Header("编队卡牌")]
        private FormationCardItem[] formationCards;
        private int _currentFormationCardIndex = 0;//默认从卡牌0开始
        
        private int _currentLevelId = 0;//当前关卡id
        private bool _isPvpMode;
        private PvpPrepareData _pvpPrepareData;
        private BattleNetworkController _battleNetwork;

        public string targetSelectCharacterName = string.Empty;//记录滚动列表当前选择的角色

        protected override void OnAwake()
        {
            Find<Button>("Btn_return").onClick.AddListener(onReturnBtn);
            Find<Button>("Btn_action").onClick.AddListener(onActionBtn);
            _battleNetwork = new BattleNetworkController();

            levelTargetText1 = Find<TextMeshProUGUI>("LevelDetailArea/Target/Img_content1/Txt_content1");
            levelTargetText2 = Find<TextMeshProUGUI>("LevelDetailArea/Target/Img_content2/Txt_content2");
            selectFormationArea = Find<Transform>("SelectFormationArea");
            
            bindFormationBtn();
            Find<Button>("SelectFormationArea/Btn_confirm").onClick.AddListener(onFormationConfirmBtn);
            Find<Button>("SelectFormationArea/Btn_cancel").onClick.AddListener(onFormationCancelBtn);
        }
        
        private void bindFormationBtn()
        {
            int cardCount = 4;
            formationCards = new FormationCardItem[cardCount];

            for (int i = 0; i < cardCount; i++)
            {
                Transform cardTf = Find<Transform>($"FormationArea/Card_{i}");
                
                FormationCardItem item = new FormationCardItem();
                item.Init(cardTf, i, onFormationCardBtn);
                
                formationCards[i] = item;//无论是否有卡牌都存入
            }
        }

        public override void Open(params object[] args)
        {
            _battleNetwork.InitPvpPrepare();
            GameApp.MessageCenter.AddEvent(EventDefines.OnPvpTeamWaiting, onPvpTeamWaiting);
            GameApp.MessageCenter.AddEvent(EventDefines.OnPvpBattleStart, onPvpBattleStart);

            if (args.Length > 0 && args[0] is PvpPrepareData pvpPrepareData)
            {
                openPvpPrepare(pvpPrepareData);
                return;
            }

            openPvePrepare(args);
        }

        public override void Close(params object[] args)
        {
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpTeamWaiting, onPvpTeamWaiting);
            GameApp.MessageCenter.RemoveEvent(EventDefines.OnPvpBattleStart, onPvpBattleStart);
            _battleNetwork.UnInitPvpPrepare();
            base.Close(args);
        }

        // 打开PVE准备界面
        private void openPvePrepare(params object[] args)
        {
            _isPvpMode = false;
            _pvpPrepareData = null;
            _currentLevelId = (int)args[0];
            LevelDataSO dataSo = GameApp.ConfigManager.Level.Get(_currentLevelId);
            if (dataSo == null)
            {
                QLog.Error("未找到关卡数据, id: " + _currentLevelId);
                return;
            }

            string[] desParts = dataSo.Description.Split('-');
            if (desParts.Length >= 2)
            {
                levelTargetText1.text = desParts[0];
                levelTargetText2.text = desParts[1];
            }
        }

        // 打开PVP准备界面
        private void openPvpPrepare(PvpPrepareData pvpPrepareData)
        {
            _isPvpMode = true;
            _pvpPrepareData = pvpPrepareData;
            levelTargetText1.text = "选择你的PvP阵容";
            levelTargetText2.text = $"玩家编号：{pvpPrepareData.PlayerId}";
        }

        private void onReturnBtn()
        {
            if (_isPvpMode)
                _battleNetwork.SendLeavePvp();

            GameApp.ViewManager.NavigateBack();
        }

        private void onActionBtn()
        {
            if (_isPvpMode)
            {
                _battleNetwork.SendSubmitPvpTeam(getSelectedHeroIds());
                levelTargetText1.text = "阵容已提交";
                levelTargetText2.text = "等待对手确认阵容...";
                return;
            }

            ViewExtensions.LoadScene(this, SceneDefines.Fight,() =>
            {
                ApplyControllerFunc(ControllerType.Fight, EventDefines.OpenFightingView, GetLevelInitData());
            });
        }

        private LevelModel GetLevelInitData()
        {
            List<CharacterDataSO> characters = getSelectedCharacters();
            List<MonsterSpawnData> monsterSpawnData = GameApp.ConfigManager.Level.GetLevelMonsterSpawnData(_currentLevelId);
            LevelModel model = new LevelModel(characters, monsterSpawnData, _currentLevelId);

            return model;
        }

        // 获取当前编队角色
        private List<CharacterDataSO> getSelectedCharacters()
        {
            List<CharacterDataSO> characters = new List<CharacterDataSO>();
            for (int i = 0; i < formationCards.Length; i++)
            {
                string cardName = formationCards[i].GetCardName();
                CharacterDataSO data = GameApp.ConfigManager.Character.GetByName(cardName);
                if(data == null) QLog.Info("未找到角色数据, name: " + cardName);
                characters.Add(data);
            }

            return characters;
        }

        // 获取当前编队角色配置Id
        private List<int> getSelectedHeroIds()
        {
            List<int> heroIds = new List<int>();
            List<CharacterDataSO> characters = getSelectedCharacters();
            for (int i = 0; i < characters.Count; i++)
            {
                if (characters[i] != null)
                    heroIds.Add(characters[i].Id);
            }

            return heroIds;
        }

        // 处理PVP等待对手提交
        private void onPvpTeamWaiting(object arg)
        {
            levelTargetText1.text = "阵容已提交";
            levelTargetText2.text = "等待对手确认阵容...";
        }

        // 处理PVP战斗开始
        private void onPvpBattleStart(object arg)
        {
            ViewExtensions.LoadScene(this, SceneDefines.Fight,() =>
            {
                ApplyControllerFunc(ControllerType.Fight, EventDefines.OpenFightingView, arg as PvpBattleStartData);
            });
        }

        #region 编队相关
        //提供给滚动列表查询当前编队卡牌数据的接口
        public string GetCurrentFormationCardName()
        {
            if (formationCards[_currentFormationCardIndex] != null)
                return formationCards[_currentFormationCardIndex].GetCardName();
            return String.Empty;
        }
        
        //刷新卡牌UI
        private void updateFormationCardUI(int index, string name, Sprite spr)
        {
            formationCards[index].RefreshData(name, spr);
        }
        
        private void onFormationCardBtn(int index)
        {
            _currentFormationCardIndex = index;
            targetSelectCharacterName = formationCards[index].GetCardName();
            setSelectFormationAreaActive(true);
        }
        #endregion
        
        #region 滚动列表相关

        private void setSelectFormationAreaActive(bool active)
        {
            selectFormationArea.gameObject.SetActive(active);

            if (active)
            {
                LoopVerticalScrollRect rect = Find<LoopVerticalScrollRect>("SelectFormationArea/Scroll_character");
                rect.RefreshCells();//每次打开编队选择时刷新列表
            }
        }
        
        //查询卡牌自身状态
        public int CheckCharacterState(string charName)
        {
            //0为当前选择，1为已选但非当前，2为未选择
            
            if (string.IsNullOrEmpty(charName)) return 2;

            for (int i = 0; i < formationCards.Length; i++)
            {
                if (formationCards[i] != null && formationCards[i].GetCardName() == charName)
                {
                    if (i == _currentFormationCardIndex) return 0;
                    return 1;
                }
            }

            return 2;
        }

        //选择卡牌后回调函数
        public void OnSelectCharacterFromScroll(string charName)
        {
            targetSelectCharacterName = charName;
            
            LoopVerticalScrollRect rect = Find<LoopVerticalScrollRect>("SelectFormationArea/Scroll_character");
            rect.RefreshCells();
        }
        
        private void onFormationConfirmBtn()
        {
            //情况一：没有更换卡牌,保持不变(name与此次索引的编队卡牌name相同)
            //情况二：更换了卡牌,但之前这个索引的编队为空 && 更换的卡牌不在编队中,则直接替换
            //情况三：更换了卡牌,但之前这个索引的编队为空 && 更换的卡牌在编队中,则需要将更换的卡牌所在的索引卡牌数据清空,再将更换的卡牌数据替换
            //情况四：更换了卡牌,但之前这个索引的编队不为空 && 更换的卡牌不在编队中,则直接替换
            //情况五：更换了卡牌,但之前这个索引的编队不为空 && 更换的卡牌在编队中,则需要将更换的卡牌所在的索引卡牌数据替换到之前这个索引,再将更换的卡牌数据替换
            
            selectFormationArea.gameObject.SetActive(false);
            
            if(string.IsNullOrEmpty(targetSelectCharacterName))
                return;

            string targetName = targetSelectCharacterName;
            string currentSlotName = formationCards[_currentFormationCardIndex].GetCardName();
            
            //情况一:没有发生任何改变
            if(targetName == currentSlotName)
                return;

            int exitIndex = -1;
            //查找新选择的角色是否已在队伍中(不包括自己)
            for (int i = 0; i < formationCards.Length; i++)
            {
                if (i != _currentFormationCardIndex && formationCards[i].GetCardName() == targetName)
                {
                    exitIndex = i;
                    break;
                }
            }

            //情况三、五
            if (exitIndex != -1)
            {
                //角色已经在该队伍中了
                updateFormationCardUI(exitIndex, currentSlotName, null);
            }
            
            //情况二、四
            updateFormationCardUI(_currentFormationCardIndex, targetName, null);

            targetSelectCharacterName = string.Empty;//清空选中状态
        }

        private void onFormationCancelBtn()
        {
            selectFormationArea.gameObject.SetActive(false);
            targetSelectCharacterName = string.Empty;
        }
        #endregion
    }
}