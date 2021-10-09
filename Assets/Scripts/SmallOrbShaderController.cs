using UnityEngine;

using System;
using System.Collections;

using TMPro;

public class SmallOrbShaderController : MonoBehaviour
{
  private const float MIN_GROWTH_STEEPNESS = 0f;
  private const float MAX_GROWTH_STEEPNESS = 15f;

  private const float MIN_GROWTH_SHAPE = 0.5f;
  private const float MAX_GROWTH_SHAPE = 5f;

  private const float MIN_APPROACHING_STEEPNESS = 0.1f;
  private const float MAX_APPROACHING_STEEPNESS = 1f;

  [Header("Size Values")]
  [SerializeField] private CurveType m_curveTypeSize = CurveType.Growth;
  [SerializeField, Range(MIN_GROWTH_STEEPNESS, MAX_GROWTH_STEEPNESS)] private float m_growthSteepnessSize = 0f;
  [SerializeField, Range(MIN_GROWTH_SHAPE, MAX_GROWTH_SHAPE)] private float m_growthShapeSize = 2f;
  [SerializeField, Range(MIN_APPROACHING_STEEPNESS, MAX_APPROACHING_STEEPNESS)] private float m_approachingSteepnessSize = 0.5f;
  [SerializeField, Range(0f, 50f)] private float m_minSize = .1f, m_maxSize = 8f;

  [Header("Intensity Values")]
  [SerializeField] private CurveType m_curveTypeIntensity = CurveType.Linear;
  [SerializeField, Range(MIN_GROWTH_STEEPNESS, MAX_GROWTH_STEEPNESS)] private float m_growthSteepnessIntensity = 0f;
  [SerializeField, Range(MIN_GROWTH_SHAPE, MAX_GROWTH_SHAPE)] private float m_growthShapeIntensity = 2f;
  [SerializeField, Range(MIN_APPROACHING_STEEPNESS, MAX_APPROACHING_STEEPNESS)] private float m_approachingSteepnessIntensity = 0.5f;
  [SerializeField, Range(-3f, 3f)] private float m_minIntensity = -2f, m_maxIntensity = 2f;

  // Whether it has been initialised already, for safety
  private bool m_isInitialised = false;

  // Whether it is at the end of the timeline
  private bool m_endOfTimeline = false;
  // Percentage of progress between the dates
  private float m_percentageProgress;

  // Category this orb belongs to
  private Category m_category;
  // Word this orb has
  private string m_word;
  // Cache the material of the orb
  private Material m_shaderMaterial;
  [ColorUsage(true, true)] private Color m_defaultColour;
  // Whether this orb's story point is active
  private bool m_hasStoryPointActive = false;

  public bool IsHovered;
  private Wiggle m_wiggle;
  public TextMeshPro m_nameDisplayer;

  // Cache
  //private StoryPointInterface m_storyPointInterface;
  //private StoryPoint? m_storyPoint;
  private bool m_hasStoryPoint = false;
  private DateTime m_storyStartDate, m_storyEndDate;
  
  public bool m_active;

  ////////////////////
  // Parameters 

  // Max counts of parameters
  private int m_maxWordCount;
  private int m_maxChangeInWordCount;

  // Cache
  private int m_currentWordCount;
  private int m_nextWordCount;
  private int m_currentChangeInWordCount;
  private int m_nextChangeInWordCount;

  private void Awake()
  {
    m_shaderMaterial = GetComponent<Renderer>().material;
    m_wiggle = GetComponent<Wiggle>();
    m_nameDisplayer = GetComponentInChildren<TextMeshPro>();

    TimeManager.Instance.DateChangeEvent += OnDateChanged;
  }

  private void OnEnable()
  {
    TimeManager.Instance.TimeAdvanceEvent += OnTimeAdvance;
  }

  private void OnDisable()
  {
    if (TimeManager.Instance)
    {
      TimeManager.Instance.TimeAdvanceEvent -= OnTimeAdvance;
    }
  }

  private void Update()
  {
    if (m_active)
    {
      if (m_hasStoryPointActive)
      {
        m_wiggle.active = false;
        m_nameDisplayer.text = m_word + ", " +
          DataManager.Instance.GetWordCount(m_category, m_word, TimeManager.Instance.ConvertCurrentDayToDateTime())
            .ToString() + " mentions";
        m_shaderMaterial.SetFloat("Vector1_D15D7337", 1);
      }
      else if (IsHovered)
      {
        m_wiggle.active = false;
        m_nameDisplayer.text =
          DataManager.Instance.GetWordCount(m_category, m_word, TimeManager.Instance.ConvertCurrentDayToDateTime())
            .ToString() + " mentions";
        m_shaderMaterial.SetFloat("Vector1_D15D7337", 1);
      }
      else
      {
        m_wiggle.active = true;
        m_nameDisplayer.text = m_word;
        m_shaderMaterial.SetFloat("Vector1_D15D7337", 0);
      }
    }
    else
    {
      m_nameDisplayer.text = "";
    }

    IsHovered = false;
    
    if (!m_endOfTimeline)
    {
      UpdateShader();
    }
  }

  private void UpdateShader()
  {
    UpdateSize();
    UpdateColour();
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

    m_shaderMaterial.SetFloat("Vector1_C382F479", sizeValue);

    //UpdateIntensity(actualRatio);
  }

  //private void UpdateIntensity(float ratio)
  //{
  //  float intensityValue = 0;

  //  if (m_curveTypeIntensity == CurveType.Linear)
  //  {
  //    intensityValue = Mathf.Lerp(m_minIntensity, m_maxIntensity, ratio);
  //  }

  //  else if (m_curveTypeIntensity == CurveType.Growth)
  //  {
  //    intensityValue = (m_maxIntensity - m_minIntensity) * Mathf.Pow(ratio, m_growthSteepnessIntensity * ratio + m_growthShapeIntensity) + m_minIntensity;
  //  }

  //  else if (m_curveTypeIntensity == CurveType.Approaching)
  //  {
  //    intensityValue = (m_maxIntensity - m_minIntensity) * Mathf.Pow(ratio, m_approachingSteepnessIntensity) + m_minIntensity;
  //  }

  //  float factor = Mathf.Pow(2, intensityValue);
  //  Color newIntensity = new Color(m_defaultColour.r * factor, m_defaultColour.g * factor, m_defaultColour.b * factor);

  //  m_shaderMaterial.SetColor("Color_2286D43A", newIntensity);
  //}

  private void UpdateColour()
  {
    // REMEMBER TO CAST TO FLOAT
    float currentRatio = (float)m_currentChangeInWordCount / (float)m_maxChangeInWordCount;
    float targetRatio = (float)m_nextChangeInWordCount / (float)m_maxChangeInWordCount;
    float actualRatio = Mathf.Lerp(currentRatio, targetRatio, m_percentageProgress);

    // Normalise the ratio from 0 - 1
    actualRatio += 1f;
    actualRatio *= 0.5f;

    float intensityValue = 0;

    if (m_curveTypeIntensity == CurveType.Linear)
    {
      intensityValue = Mathf.Lerp(m_minIntensity, m_maxIntensity, actualRatio);
    }

    else if (m_curveTypeIntensity == CurveType.Growth)
    {
      intensityValue = (m_maxIntensity - m_minIntensity) * Mathf.Pow(actualRatio, m_growthSteepnessIntensity * actualRatio + m_growthShapeIntensity) + m_minIntensity;
    }

    else if (m_curveTypeIntensity == CurveType.Approaching)
    {
      intensityValue = (m_maxIntensity - m_minIntensity) * Mathf.Pow(actualRatio, m_approachingSteepnessIntensity) + m_minIntensity;
    }

    float factor = Mathf.Pow(2, intensityValue);
    Color newIntensity = new Color(m_defaultColour.r * factor, m_defaultColour.g * factor, m_defaultColour.b * factor);

    m_shaderMaterial.SetColor("Color_2286D43A", newIntensity);
  }

  public void Initialise( Category categoy, string word, bool hasStoryPoint, DateTime? startDate, DateTime? endDate )
  {
    if (!m_isInitialised)
    {
      m_category = categoy;
      m_word = word;

      m_maxWordCount = DataManager.Instance.GetMaxWordCountOfWordOfCategory(m_category, m_word);
      m_maxChangeInWordCount = DataManager.Instance.GetMaxChangeInWordCountOfWordOfCategory(m_category, m_word);

      m_hasStoryPoint = hasStoryPoint;

      if (startDate != null && endDate != null)
      {
        m_storyStartDate = (DateTime)startDate;
        m_storyEndDate = (DateTime)endDate;
      }
    }
  }

  private void OnTimeAdvance( float t )
  {
    m_percentageProgress = t;
  }

  private void OnDateChanged( DateTime currentDate, DateTime nextDate )
  {
    int compare = DateTime.Compare(nextDate, currentDate);
    bool forward = compare > 0;

    if (compare == 0)
    {
      m_endOfTimeline = true;
    }

    else
    {
      m_endOfTimeline = false;
    }

    m_currentWordCount = DataManager.Instance.GetWordCount(m_category, m_word, currentDate);
    m_nextWordCount = DataManager.Instance.GetWordCount(m_category, m_word, nextDate);

    m_currentChangeInWordCount = m_nextChangeInWordCount;
    m_nextChangeInWordCount = m_nextWordCount - m_currentWordCount;

    CheckForStoryPointTrigger(currentDate, forward);
  }

  private void CheckForStoryPointTrigger( DateTime currentDate, bool forward )
  {
    if (m_hasStoryPoint)
    {
      if (DateTime.Compare(m_storyStartDate, currentDate) == 0)
      {
        if (forward)
        {
          m_hasStoryPointActive = true;
          Debug.Log("Start StoryPoint triggered! " + currentDate.ToString("dd MMMM yyyy"));
        }

        else
        {
          m_hasStoryPointActive = false;
          Debug.Log("End StoryPoint triggered! " + currentDate.ToString("dd MMMM yyyy"));
        }
      }

      else if (DateTime.Compare(m_storyEndDate, currentDate) == 0)
      {
        if (forward)
        {
          m_hasStoryPointActive = true;
          Debug.Log("End StoryPoint triggered! " + currentDate.ToString("dd MMMM yyyy"));
        }

        else
        {
          m_hasStoryPointActive = false;
          Debug.Log("Start StoryPoint triggered! " + currentDate.ToString("dd MMMM yyyy"));
        }
      }
    }
  }

  public string GetWord()
  {
    return m_word;
  }

  public void SetShaderColour(Color colour)
  {
    m_defaultColour = colour;

    m_shaderMaterial.SetColor("Color_2286D43A", m_defaultColour);
  }
}
