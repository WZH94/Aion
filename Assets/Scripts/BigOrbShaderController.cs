using UnityEngine;

using System;
using System.Collections;

public class BigOrbShaderController : MonoBehaviour
{
  private const float MIN_GROWTH_STEEPNESS = 0f;
  private const float MAX_GROWTH_STEEPNESS = 15f;

  private const float MIN_GROWTH_SHAPE = 0.5f;
  private const float MAX_GROWTH_SHAPE = 5f;

  private const float MIN_APPROACHING_STEEPNESS = 0.1f;
  private const float MAX_APPROACHING_STEEPNESS = 1f;

  [Header("Displacement Values")]
  [SerializeField] private CurveType m_curveTypeDisplacement = CurveType.Linear;
  [SerializeField, Range(MIN_GROWTH_STEEPNESS, MAX_GROWTH_STEEPNESS)] private float m_growthSteepnessDisplacement = 0f;
  [SerializeField, Range(MIN_GROWTH_SHAPE, MAX_GROWTH_SHAPE)] private float m_growthShapeDisplacement = 2f;
  [SerializeField, Range(MIN_APPROACHING_STEEPNESS, MAX_APPROACHING_STEEPNESS)] private float m_approachingSteepnessDisplacement = 0.5f;
  [SerializeField, Range(1f, 20f)] private float m_minDisplacement = 1f, m_maxDisplacement = 10f;

  [Header("Transparency Values")]
  [SerializeField] private CurveType m_curveTypeTransparency = CurveType.Growth;
  [SerializeField, Range(MIN_GROWTH_STEEPNESS, MAX_GROWTH_STEEPNESS)] private float m_growthSteepnessTransparency = 0f;
  [SerializeField, Range(MIN_GROWTH_SHAPE, MAX_GROWTH_SHAPE)] private float m_growthShapeTransparency = 2f;
  [SerializeField, Range(MIN_APPROACHING_STEEPNESS, MAX_APPROACHING_STEEPNESS)] private float m_approachingSteepnessTransparency = 0.5f;
  [SerializeField, Range(0f, 1f)] private float m_minTransparency = 0.65f, m_maxTransparency = 0.15f;

  [Header("Size Values")]
  [SerializeField] private CurveType m_curveTypeSize = CurveType.Growth;
  [SerializeField, Range(MIN_GROWTH_STEEPNESS, MAX_GROWTH_STEEPNESS)] private float m_growthSteepnessSize = 0f;
  [SerializeField, Range(MIN_GROWTH_SHAPE, MAX_GROWTH_SHAPE)] private float m_growthShapeSize = 2f;
  [SerializeField, Range(MIN_APPROACHING_STEEPNESS, MAX_APPROACHING_STEEPNESS)] private float m_approachingSteepnessSize = 0.5f;
  [SerializeField, Range(1f, 80f)] private float m_minSize = 3f, m_maxSize = 15f;

  [Header("Others")]
  [SerializeField, Range(2f, 10f)] private float m_timeLerpSpeed = 5f;
  private float m_currentTimeSpeed = 1f;
  private float m_targetTimeSpeed = 1f;

  // Whether it has been initialised already, for safety
  private bool m_isInitialised = false;

  // Category this orb belongs to
  private Category m_category;
  // Cache the material of the orb
  private Material m_shaderMaterial;

  // Whether it is at the end of the timeline
  private bool m_endOfTimeline = false;
  // Percentage of progress between the dates
  private float m_percentageProgress;

  ////////////////////
  // Parameters 

  // Max counts of parameters
  private int m_maxWordCount;
  private int m_maxNumberOfWords;

  // Cache
  private int m_currentWordCount;
  private int m_currentNumberOfWords;
  private int m_nextWordCount;
  private int m_nextNumberOfWords;
  [ColorUsage(true, true)]private Color m_colour;

  private void Start()
  {
    TimeManager.Instance.DateChangeEvent += OnDateChanged;
    TimeManager.Instance.TimeAdvanceEvent += OnTimeAdvance;
    TimeManager.Instance.SpeedChangeEvent += OnSpeedChanged;

    m_shaderMaterial.SetColor("Color_A4F22710", m_colour);
  }

  private void Update()
  {
    if (!m_endOfTimeline)
    {
      UpdateShader();
    }
  }

  public void Initialise(Category category)
  {
    if (!m_isInitialised)
    {
      m_isInitialised = true;
      m_category = category;

      m_maxWordCount = DataManager.Instance.GetMaxWordCountInCategory(m_category);
      m_maxNumberOfWords = DataManager.Instance.GetMaxNumberOfWordsInCategory(m_category);

      m_shaderMaterial = GetComponent<MeshRenderer>().material;
      m_shaderMaterial.SetFloat("Vector1_C300DB3", 1f);
      m_currentTimeSpeed = 1f;
      m_targetTimeSpeed = 1f;
    }
  }

  public void SetShaderColour(Color colour)
  {
    m_colour = colour;
  }

  private void UpdateShader()
  {
    UpdateSize();
    UpdateTransparency();
    UpdateDisplacement();
  }

  private void UpdateSize()
  {
    // REMEMBER TO CAST TO FLOAT
    float currentRatio = (float)m_currentWordCount / (float)m_maxWordCount;
    float targetRatio = (float)m_nextWordCount / (float)m_maxWordCount;
    float actualRatio = Mathf.Lerp(currentRatio, targetRatio, m_percentageProgress);

    float sizeValue = 0;

    if (m_curveTypeSize == CurveType.Linear)
    {
      sizeValue = Mathf.Lerp(m_minSize, m_maxSize, actualRatio);
    }
     
    else if (m_curveTypeSize == CurveType.Growth)
    {
      sizeValue = (m_maxSize - m_minSize) * Mathf.Pow(actualRatio, m_growthSteepnessSize * actualRatio + m_growthShapeSize) + m_minSize;
    }

    else if (m_curveTypeSize == CurveType.Approaching)
    {
      sizeValue = (m_maxSize - m_minSize) * Mathf.Pow(actualRatio, m_approachingSteepnessSize) + m_minSize;
    }

    //if (m_category == Category.Economy)
    //Debug.Log("Size: " + m_minSize + " " + m_maxSize + " " + sizeValue + " " + actualRatio);

    m_shaderMaterial.SetFloat("Vector1_924ABE9", sizeValue);
  }

  private void UpdateTransparency()
  {
    // REMEMBER TO CAST TO FLOAT
    float currentRatio = (float)m_currentWordCount / (float)m_maxWordCount;
    float targetRatio = (float)m_nextWordCount / (float)m_maxWordCount;
    float actualRatio = Mathf.Lerp(currentRatio, targetRatio, m_percentageProgress);

    float transparencyValue = 0;

    if (m_curveTypeTransparency == CurveType.Linear)
    {
      transparencyValue = Mathf.Lerp(m_minTransparency, m_maxTransparency, actualRatio);
    }

    else if (m_curveTypeTransparency == CurveType.Growth)
    {
      transparencyValue = (m_maxTransparency - m_minTransparency) * Mathf.Pow(actualRatio, m_growthSteepnessTransparency * actualRatio + m_growthShapeTransparency) + m_minTransparency;
    }

    else if (m_curveTypeTransparency == CurveType.Approaching)
    {
      transparencyValue = (m_maxTransparency - m_minTransparency) * Mathf.Pow(actualRatio, m_approachingSteepnessTransparency) + m_minTransparency;
    }

    //Debug.Log("Transparency: " + m_minTransparency + " " + m_maxTransparency + " " + transparencyValue);

    m_shaderMaterial.SetFloat("Vector1_1C466FB9", transparencyValue);
  }

  private void UpdateDisplacement()
  {
    // REMEMBER TO CAST TO FLOAT
    float currentRatio = (float)m_currentNumberOfWords / (float)m_maxNumberOfWords;
    float targetRatio = (float)m_nextNumberOfWords / (float)m_maxNumberOfWords;
    float actualRatio = Mathf.Lerp(currentRatio, targetRatio, m_percentageProgress);

    float displacementValue = 0;

    if (m_curveTypeDisplacement == CurveType.Linear)
    {
      displacementValue = Mathf.Lerp(m_minDisplacement, m_maxDisplacement, actualRatio);
    }

    else if (m_curveTypeDisplacement == CurveType.Growth)
    {
      displacementValue = (m_maxDisplacement - m_minDisplacement) * Mathf.Pow(actualRatio, m_growthSteepnessDisplacement * actualRatio + m_growthShapeDisplacement) + m_minDisplacement;
    }

    else if (m_curveTypeDisplacement == CurveType.Approaching)
    {
      displacementValue = (m_maxDisplacement - m_minDisplacement) * Mathf.Pow(actualRatio, m_approachingSteepnessDisplacement) + m_minDisplacement;
    }

    //Debug.Log("Displacement: " + m_minDisplacement + " " + m_maxDisplacement + " " + displacementValue);

    m_shaderMaterial.SetFloat("Vector1_BB8142BE", displacementValue);
  }

  private void OnTimeAdvance(float t)
  {
    m_percentageProgress = t;
  }

  private void OnDateChanged(DateTime currentDate, DateTime nextDate)
  {
    if (DateTime.Compare(currentDate, nextDate) == 0)
    {
      m_endOfTimeline = true;
    }

    else
    {
      m_endOfTimeline = false;
    }

    m_currentWordCount = DataManager.Instance.GetTotalWordCountOnDateInCategory(m_category, currentDate);
    m_nextWordCount = DataManager.Instance.GetTotalWordCountOnDateInCategory(m_category, nextDate);

    //m_currentNumberOfWords = DataManager.Instance.GetNumberOfActiveWordsInCategory(m_category, currentDate);
    //m_nextNumberOfWords = DataManager.Instance.GetNumberOfActiveWordsInCategory(m_category, nextDate);
  }

  private void OnSpeedChanged(float speed)
  {
    m_targetTimeSpeed = speed;

    StartCoroutine(LerpTimeSpeedShader());
  }

  private IEnumerator LerpTimeSpeedShader()
  {
    while (Mathf.Abs(m_currentTimeSpeed - m_targetTimeSpeed) >= 0.01f)
    {
      m_currentTimeSpeed = Mathf.SmoothStep(m_currentTimeSpeed, m_targetTimeSpeed, m_timeLerpSpeed * Time.deltaTime);
      m_shaderMaterial.SetFloat("Vector1_C300DB3", m_currentTimeSpeed);

      yield return null;
    }

    m_currentTimeSpeed = m_targetTimeSpeed;
    m_shaderMaterial.SetFloat("Vector1_C300DB3", m_currentTimeSpeed);
  }
}
