using System;
using System.Collections;

using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenScript : MonoBehaviour
{
  [Header("Settings")] 
  [SerializeField] private int sceneToLoad = 2;
  [SerializeField] private float pauseDuration;
  [SerializeField] private float screen1Duration; 
  [SerializeField] private float screen2Duration;
  [SerializeField] private float minLoadingSpeed = 0.00001f;
  [SerializeField] private float maxLoadingSpeed = 0.0001f;

  [Header("UI Elements")]
  [SerializeField] private GameObject screen1;
  [SerializeField] private GameObject screen2;
  [SerializeField] private Image progressBar;
  [SerializeField] private TextMeshProUGUI loadingText;
  [SerializeField] private TextMeshProUGUI progressText;

  private bool _canLeave;
  private float _fakeProgress;


  // Start is called before the first frame update
  void Start()
  {
    _canLeave = false;
    _fakeProgress = 0;
    screen1.SetActive(false);
    screen2.SetActive(false);
    
    //start async operation
    StartCoroutine(LoadScene());
    StartCoroutine(MenuSequence());
  }

  private void updateProgressBar()
  {
    _fakeProgress += Random.Range(minLoadingSpeed, maxLoadingSpeed);
  }

  IEnumerator MenuSequence()
  {
   screen1.SetActive(true);
   yield return new WaitForSeconds(screen1Duration);
   screen1.SetActive(false);
   yield return new WaitForSeconds(pauseDuration);
   screen2.SetActive(true);
   yield return new WaitForSeconds(screen2Duration);
   _canLeave = true;
  }

  IEnumerator LoadScene()
  {
    //create async operation
    AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Single);
    asyncOperation.allowSceneActivation = false;
        
    while (!asyncOperation.isDone)
    {
      //Output the current progress
      loadingText.text = "Loading";
      updateProgressBar();
      progressText.text = System.Math.Round((_fakeProgress * 100),1) + "%";
      progressBar.fillAmount = _fakeProgress;

      // Check if the load has finished
      if (_fakeProgress >= 0.9f)
      {
        //Change the Text to show the Scene is ready
        progressBar.fillAmount = 1;
        loadingText.text = "Please wait";
        progressText.text = "100%";
        //Wait to you press any key to activate the Scene
        if (_canLeave)
        {
          loadingText.text = "Press any button to continue";
          if (Input.anyKey)
          {
            //Activate the Scene
            asyncOperation.allowSceneActivation = true;
          }
        }
      }

      yield return null;
    }
  }
}
