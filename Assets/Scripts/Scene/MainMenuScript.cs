using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{

  [SerializeField] private TMP_Text versionText;

  private void Start()
  {
    versionText.text = "Version: " + Application.version;
  }

  public void EnterLoadingScreen()
  {
    SceneManager.LoadScene(1, LoadSceneMode.Single);
  }
    
  public void ExitApplication()
  {
    Application.Quit();
  }
}
