using System.Collections.Generic;
using UnityEngine;

namespace MemeFighter
{
    /// <summary>Player settings, saved with PlayerPrefs.</summary>
    public static class GameSettings
    {
        public static float Master, Music, Sfx, Voice;
        public static bool Fullscreen, VSync, ScreenShake;
        /// <summary>Index into <see cref="Resolutions"/>, or -1 to keep the current one.</summary>
        public static int ResolutionIndex;

        public static void ResetDefaults()
        {
            Master = 0.8f; Music = 0.6f; Sfx = 1f; Voice = 0.9f;
            Fullscreen = true; VSync = false; ScreenShake = true;
            ResolutionIndex = -1;
        }

        public static void Load()
        {
            ResetDefaults();
            Master = PlayerPrefs.GetFloat("mf.master", Master);
            Music = PlayerPrefs.GetFloat("mf.music", Music);
            Sfx = PlayerPrefs.GetFloat("mf.sfx", Sfx);
            Voice = PlayerPrefs.GetFloat("mf.voice", Voice);
            Fullscreen = PlayerPrefs.GetInt("mf.fullscreen", Fullscreen ? 1 : 0) == 1;
            VSync = PlayerPrefs.GetInt("mf.vsync", VSync ? 1 : 0) == 1;
            ScreenShake = PlayerPrefs.GetInt("mf.shake", ScreenShake ? 1 : 0) == 1;
            ResolutionIndex = PlayerPrefs.GetInt("mf.res", ResolutionIndex);
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat("mf.master", Master);
            PlayerPrefs.SetFloat("mf.music", Music);
            PlayerPrefs.SetFloat("mf.sfx", Sfx);
            PlayerPrefs.SetFloat("mf.voice", Voice);
            PlayerPrefs.SetInt("mf.fullscreen", Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt("mf.vsync", VSync ? 1 : 0);
            PlayerPrefs.SetInt("mf.shake", ScreenShake ? 1 : 0);
            PlayerPrefs.SetInt("mf.res", ResolutionIndex);
            PlayerPrefs.Save();
        }

        /// <summary>Applies audio and display settings. Music/SFX/Voice are stored for an AudioMixer later.</summary>
        public static void Apply()
        {
            AudioListener.volume = Master;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            var res = Resolutions;
            if (ResolutionIndex >= 0 && ResolutionIndex < res.Length)
                Screen.SetResolution(res[ResolutionIndex].width, res[ResolutionIndex].height, mode);
            else
                Screen.fullScreenMode = mode;
        }

        /// <summary>Screen resolutions without duplicate refresh rates.</summary>
        public static Resolution[] Resolutions
        {
            get
            {
                var list = new List<Resolution>();
                foreach (var r in Screen.resolutions)
                    if (!list.Exists(x => x.width == r.width && x.height == r.height))
                        list.Add(r);
                return list.ToArray();
            }
        }

        public static int CurrentResolutionIndex()
        {
            var res = Resolutions;
            if (ResolutionIndex >= 0 && ResolutionIndex < res.Length) return ResolutionIndex;
            for (int i = 0; i < res.Length; i++)
                if (res[i].width == Screen.width && res[i].height == Screen.height) return i;
            return res.Length - 1;
        }
    }
}
