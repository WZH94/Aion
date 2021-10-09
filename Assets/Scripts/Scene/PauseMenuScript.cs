using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuScript : MonoBehaviour
{
  public void ReturnToMenu()
  {
    SceneManager.LoadScene(0, LoadSceneMode.Single);
  }
}
