using UnityEngine;
using UnityEngine.Video;

using TMPro;

public class StoryPointInterface : MonoBehaviour
{
  [SerializeField] private TextMeshProUGUI m_description;
  [SerializeField] private VideoPlayer m_videoPlayer;

  public void SetInterfaceDetails(StoryPoint storyPoint)
  {
    gameObject.SetActive(true);

    m_description.text = storyPoint.Description;
    m_videoPlayer.clip = storyPoint.VideoClip;

    TimeManager.Instance.SetPause(true);
  }

  public void CloseInterface()
  {
    gameObject.SetActive(false);
    TimeManager.Instance.SetPause(false);
  }
}
