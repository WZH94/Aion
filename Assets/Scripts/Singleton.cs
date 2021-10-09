using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
  private static bool m_singletonInitialised = false;

  private static T m_instance;
  public static T Instance
  {
    get
    {
      if (m_instance == null)
      {
        T[] results = FindObjectsOfType<T>();

        if (!m_singletonInitialised )
        {
          if (!m_singletonInitialised && results.Length == 0)
          {
            Debug.LogWarning("Singleton TimeManager has no active instance!");

            return null;
          }

          else if (!m_singletonInitialised && results.Length > 1)
          {
            Debug.LogWarning("Singleton TimeManager has more than 1 active instance! Number of instances " + results.Length);

            return null;
          }

          else m_singletonInitialised = true;

          m_instance = results[0];
        }
      }

      return m_instance;
    }
  }

  protected static void ResetInstance()
  {
    m_singletonInitialised = false;
    m_instance = null;
  }
}
