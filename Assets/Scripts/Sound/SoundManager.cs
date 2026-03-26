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
                
                if (bgmSource != null)
                {
                    bgmSource.volume = _bgmVolume * _totalVolume;
                }
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
            bgmSource = GameObject.Find("Game").GetComponent<AudioSource>();
            IsStop = false;

            TotalVolume = 1f;
            BgmVolume = 1f;
            EffectVolume = 1f;
        }

        public void PlayBGM(string res)
        {
            if(isStop) return;

            if (_clipDic.TryGetValue(res, out var cachedClip))
            {
                bgmSource.clip = cachedClip;
                bgmSource.Play();
            }
            else
            {
                //AA包中加载
                ResManager.LoadAssetAsync<AudioClip>(res, (clip) =>
                {
                    if (clip == null)
                    { 
                        Debug.LogError($"该资源不存在" + res);
                        return;
                    }
                    
                    //防止异步时重复缓存
                    if (!_clipDic.ContainsKey(res))
                    {
                        _clipDic.Add(res, clip);
                    }

                    bgmSource.clip = clip;
                    bgmSource.Play();//注意:不能放在外面，否则会有时序问题，导致bgmSource.clip还未赋值就调用了Play()，从而无法播放音乐
                });
            }
        }

        public void PlayEffect(string res, Vector3 pos)
        {
            if(isStop) return;

            float currentVolume = _effectVolume * _totalVolume;
            
            if (_clipDic.ContainsKey(res))
            {
                AudioSource.PlayClipAtPoint(_clipDic[res], pos, currentVolume);
            }
            else
            {
                //AA包中加载
                ResManager.LoadAssetAsync<AudioClip>(res, (clip) =>
                {
                    if(clip == null)
                    {
                        Debug.LogError($"该资源不存在" + res);
                        return;
                    }

                    if (!_clipDic.ContainsKey(res))
                    {
                        _clipDic.Add(res, clip);
                    }

                    AudioSource.PlayClipAtPoint(clip, pos, currentVolume);
                });
            }
        }
        
    }
}