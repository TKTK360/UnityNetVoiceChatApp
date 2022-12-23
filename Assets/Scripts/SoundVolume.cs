using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UTJ.NetcodeGameObjectSample
{
    public class SoundVolume : MonoBehaviour
    {
        protected AudioSource _audio;

        // SoundVolumeの保存場所です
        public static float VoiceValue = 1.0f;


        public void Start()
        {
            _audio = GetComponent<AudioSource>();

            var slider = GetComponent<Slider>();
            if (slider != null)
            {
                VoiceValue = slider.value;
            }

            OnVoiceValueChanged(VoiceValue);
        }

        public void OnVoiceValueChanged(float val)
        {
            VoiceValue = val;

            if (_audio != null)
            {
                _audio.volume = VoiceValue;
            }
        }
        
    }
}