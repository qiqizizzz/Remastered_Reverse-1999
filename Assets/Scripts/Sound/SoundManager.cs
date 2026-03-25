/*
* ┌──────────────────────────────────┐
* │  描    述: 音乐管理器                      
* │  类    名: SoundManager.cs       
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Common;
using Common.Defines;
using UnityEngine;

namespace Sound
{
    public class SoundManager
    {
        private AudioSource bgmSource;
        private Dictionary<string, AudioClip> _clipDic;// 音频缓存字典

        private bool isStop;
        
        private float _totalVolume;//总音量
        private float _bgmVolume;//bgm音量
        private float _voiceVolume;//语音音量
        private float _effectVolume;//音效音量

        public bool IsStop
        {
            get { return isStop; }
            set
            {
                isStop = value;
                if (isStop)
                    bgmSource.Pause();
                else
                    if(!bgmSource.isPlaying)
                        bgmSource.Play();
            }
        }

        public float TotalVolume
        {
            get { return _totalVolume; }
            set
            {
                _totalVolume = value;
            }
        }
        
        public float BgmVolume
        {
            get { return _bgmVolume; }
            set
            {
                _bgmVolume = value;
                bgmSource.volume = _bgmVolume * _totalVolume;
            }
        }

        public float VoiceVolume
        {
            get { return _voiceVolume; }
            set
            {
                _voiceVolume = value;
            }
        }
        
        public float EffectVolume
        {
            get { return _effectVolume; }
            set
            {
                _effectVolume = value;
            }
        }

        public SoundManager()
        {
            _clipDic = new Dictionary<string, AudioClip>();
            bgmSource = GameObject.Find("game").GetComponent<AudioSource>();
            IsStop = false;

            TotalVolume = 1f;
            BgmVolume = 1f;
            EffectVolume = 1f;
        }

        public void PlayBGM(string res)
        {
            if(isStop) return;

            if (!_clipDic.ContainsKey(res))
            {
                //AA包中加载
                ResManager.LoadAssetAsync<AudioClip>(res, (clip) =>
                {
                    _clipDic.Add(res, clip);
                });
            }

            bgmSource.clip = _clipDic[res];
            bgmSource.Play();
        }

        public void PlayEffect(string res, Vector3 pos)
        {
            if(isStop) return;

            if (!_clipDic.ContainsKey(res))
            {
                //AA包中加载
                ResManager.LoadAssetAsync<AudioClip>(res, (clip) =>
                {
                    _clipDic.Add(res, clip);
                });
            }

            AudioSource.PlayClipAtPoint(_clipDic[res], pos);
        }
        
    }
}