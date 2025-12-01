using UnityEngine;

namespace WAD64.UI.Menu
{
  [CreateAssetMenu(menuName = "WAD64/Settings/Settings Data", fileName = "SettingsData")]
  public class SettingsData : ScriptableObject
  {
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    public float MasterVolume
    {
      get => masterVolume;
      set => masterVolume = Mathf.Clamp01(value);
    }

    public float MusicVolume
    {
      get => musicVolume;
      set => musicVolume = Mathf.Clamp01(value);
    }
  }
}


