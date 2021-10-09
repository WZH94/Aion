using UnityEngine;

using System.Collections.Generic;

public class OrbManager : Singleton<OrbManager>
{
  [Header("Orb Prefab")]
  [SerializeField] private Orb m_orbPrefab;
  [Header("Orb Instantiate Location Modifiers")]
  [SerializeField, Range(10f, 500f)] private float m_distanceFromCamera = 45f;
  [SerializeField, Range(-5f, 25f)] private float m_spawnHeight = 3f;
  [Header("Orb Pivot Information")]
  [SerializeField] private Transform origin;
  [SerializeField] private Transform m_mainCam;
  [Header("Scene References")]
  [SerializeField] private StoryPointInterface m_storyPointInterface;

  [Header("Randomisers")]
  [SerializeField] private bool m_randomiseDistance = false;
  [SerializeField, Range(1f, 100f)] private float m_distanceFromCameraPositiveRange = 10f;
  [SerializeField, Range(-100f, -1f)] private float m_distanceFromCameraNegativeRange = -10f;
  [SerializeField] private bool m_randomiseHeight = false;
  [SerializeField, Range(0.1f, 15f)] private float m_spawnHeightPositiveRange = 2f;
  [SerializeField, Range(-15f, -0.1f)] private float m_spawnHeightNegativeRange = -2f;

  private bool m_isInitialised = false;

  private Dictionary<Category,Orb> m_orbs = new Dictionary<Category, Orb>();

  // Call Initialise with dataManager after data has bee loaded and pass the categories
  public void Initialise(Category?[] categories)
  {
    if (!m_isInitialised)
    {
      m_isInitialised = true;

      // Will be retrieved from DataManager when implemented
      int numCategories = categories.Length;
      float angleBetween = 360f / numCategories;
      float radValue = Random.Range(0, 360f * Mathf.Deg2Rad);

      // Transform parent for the orbs
      GameObject orbParent = new GameObject("Orbs");
      orbParent.transform.position = origin.position;

      float currentAngle = 0f;

      for (int i = 0; i < numCategories; ++i)
      {
        Category category = (Category)categories[i];
        Dictionary<string, StoryPoint> storyPointsOfCategory = StoryPointManager.Instance.GetStoryPointsByCategory(category);

        float currentRadians = currentAngle * Mathf.Deg2Rad;

        float distanceRandomModifier = m_randomiseDistance ? Random.Range(m_distanceFromCameraNegativeRange, m_distanceFromCameraPositiveRange) : 0f;
        float spawnHeightRandomModifier = m_randomiseHeight ? Random.Range(m_spawnHeightNegativeRange, m_spawnHeightPositiveRange) : 0f;

        Vector3 normalizedVectorAngle = new Vector3(Mathf.Cos(currentRadians), 0f, Mathf.Sin(currentRadians));
        Vector3 spawnPosition = orbParent.transform.position + (normalizedVectorAngle * (m_distanceFromCamera + distanceRandomModifier) + Vector3.up * (m_spawnHeight + spawnHeightRandomModifier));

        Orb orb = Instantiate(m_orbPrefab, spawnPosition, Quaternion.identity, orbParent.transform).GetComponent<Orb>();
        orb.name = "Topic Orb " + CategoryExtension.ToString(category);
        orb.Init(origin.transform.position, category, m_mainCam, m_storyPointInterface, storyPointsOfCategory, radValue);
        orb.GetComponent<BigOrbShaderController>().Initialise(category);

        m_orbs.Add(category, orb);

        currentAngle += angleBetween;
        radValue += angleBetween * Mathf.Deg2Rad;
      }
    }
  }

  public void SetOrbStoryPoint(StoryPoint storyPoint, bool enable)
  {
    m_orbs[storyPoint.Category].SetStoryPoint(storyPoint, enable);
  }

  public Orb GetOrbWithCategory(Category category)
  {
    return m_orbs[category];
  }

  private void OnDestroy()
  {
    ResetInstance();
  }
}
