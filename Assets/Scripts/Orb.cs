using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;

using TMPro;

using Random = UnityEngine.Random;

public class Orb : MonoBehaviour
{
  [Header("Prefabs & Scene")]
  // Child orb prefab
  [SerializeField] private GameObject m_expandedOrb;

  // Textmeshpro object displaying the name
  [SerializeField] private GameObject _nameObject;
  
  // Notification object
  [SerializeField] private GameObject m_notification;
  
  [Header("Movement Modifiers")]
  // How fast it rotates around
  [SerializeField, Range(1f, 20f)] private float m_defaultMoveSpeed = 5f;
  // How fast it lerps between positions, notable when changing between selected and unselected positions
  [SerializeField, Range(1f, 20f)] private float m_lerpSpeed = 5f;
  [Header("Position Modifier")]
  // When selected, the orb zooms to the player. How far away from the camera should it be?
  [SerializeField, Range(5f, 100f)] private float m_distanceFromCamera = 20f;

  // Speed modifier from TimeManager
  private float m_currentSpeedModifier = 1f;

  // Whether orb is selected or not
  private bool m_isClicked = false;

  [Header("Sin Speed")]
  [SerializeField, Range(1f, 20f)] private float m_sinAmplitude = 3f;
  [SerializeField, Range(0.1f, 2 * Mathf.PI)] private float m_sinSpeed = 1f;
  private float m_sinRadValue = 0f;
  private float m_originalHeight = 5f;

  [Header("Colours")]
  [SerializeField, ColorUsage(true, true)] private Color m_socialColour;
  [SerializeField, ColorUsage(true, true)] private Color m_usPoliticsColour;
  [SerializeField, ColorUsage(true, true)] private Color m_globalPoliticsColour;
  [SerializeField, ColorUsage(true, true)] private Color m_healthColour;
  [SerializeField, ColorUsage(true, true)] private Color m_economyColour;

  [Header("Others")]
  // Whether orb is being hovered over or not
  public bool IsHovered = false;

  //location of player in world is passed through instantiator
  private Vector3 m_origin = new Vector3();

  private Category m_orbCategory;
  private bool m_hasActiveStoryPoint = false;
  [ColorUsage(true, true)] private Color m_orbColour;

  // Cache
  private StoryPointInterface m_storyPointInterface;
  private Dictionary<string, StoryPoint> m_storyPointsOfCategory;
  private StoryPoint? m_activeStoryPoint = null;

  private SmallOrbShaderController[] _expandedOrbs;
  private Animator[] _expandedOrbsAnimators;

  [SerializeField] private float zOffsetFromOrb;

  private bool IsClicked
  {
    get { return m_isClicked; }
    set
    {
      // Needed as m_isClicked check happens the moment LerpToPlayer coroutine is started
      bool oldValue = m_isClicked;
      m_isClicked = value;

      if (value && !oldValue)
      {
        StopAllCoroutines();
        StartCoroutine(LerpToPlayer());
      }

      else if (!value && oldValue)
      {
        StopAllCoroutines();
        CollapseChildren();
        StartCoroutine(LerpToActualPosition());
      }
    }
  }

  // The actual transform of the orb rotating around the player
  private GameObject m_actualTransform;
  // Cache VR Camera
  private Transform m_mainCamera;
  
  // Amount of orbs per ring (orbs around main orb are placed as "rings" around the orb with incrementing diameters
  private int m_orbsPerRing = 8;
  // Radius of base ring
  private float m_baseRingRadius;
  // Additional radius per new ring
  private float m_additionalRingRadius;
  // Position struct for slots
  private Vector3[] _slots;

  // Test feature
  //public bool ToggleSelected = false;
  
  //returns whether it is lerping to player or not
  public bool Toggle()
  {
    IsClicked = !m_isClicked;
    if (m_isClicked)
      return true;
    return false;
  }

  private void Awake()
  {
    // Derive expanded orb properties from expanded orb prefab
    m_baseRingRadius = m_expandedOrb.transform.localScale.x * 4f;
    m_additionalRingRadius = m_baseRingRadius * .2f;
    
    // Subscribe to event
    TimeManager.Instance.SpeedChangeEvent += SpeedModified;

    // The actual position of the orb
    m_actualTransform = new GameObject("Orb Actual Position");
    m_actualTransform.transform.position = transform.position;
    m_actualTransform.transform.parent = transform.parent;
    
    GetComponentInChildren<Canvas>().worldCamera = Camera.main.GetComponent<Camera>();

    // Auto sets the transform to the rotation around the player
    StartCoroutine(LerpToActualPosition());
  }

  public void Init(Vector3 instantiatorLocation, Category category, Transform mainCamTransform, StoryPointInterface storyPointInterface, Dictionary<string, StoryPoint> storyPoints, float sinStartingRad )
  {
    m_origin = instantiatorLocation;
    m_orbCategory = category;

    _nameObject.GetComponent<TextMeshPro>().text = CategoryExtension.ToString(category);
    
    m_mainCamera = mainCamTransform;
    m_storyPointInterface = storyPointInterface;
    m_storyPointsOfCategory = storyPoints;

    m_sinRadValue = sinStartingRad;
    m_originalHeight = m_actualTransform.transform.position.y;

    // Set the colours of the orbs here depending on the category
    SetShaderColours();

    // Create locations for slots
    CreateGrid();
    InitChildren();
    CollapseChildren();
  }

  private void SetShaderColours()
  {
    switch (m_orbCategory)
    {
      case Category.Social:
        m_orbColour = m_socialColour;
        break;

      case Category.US_Politics:
        m_orbColour = m_usPoliticsColour;
        break;

      case Category.Global_Politics:
        m_orbColour = m_globalPoliticsColour;
        break;

      case Category.Health:
        m_orbColour = m_healthColour;
        break;

      case Category.Economy:
        m_orbColour = m_economyColour;
        break;
    }

    GetComponent<BigOrbShaderController>().SetShaderColour(m_orbColour);
  }
  
  // Test function for working without data
  public void TestInit(Vector3 instantiatorLocation)
  {
    m_origin = instantiatorLocation;

    // Create locations for slots
    CreateGrid();
    
    // Temp test functions
    InstantiateTestOrbs();
  }

  public void SetStoryPoint(StoryPoint storyPoint, bool enable)
  {
    if (enable)
    {
      m_activeStoryPoint = storyPoint;
      m_hasActiveStoryPoint = true;
      m_notification.SetActive(true);
    }

    else
    {
      // Safety check, new story point may have been inserted already and we don't want to remove that
      if (m_activeStoryPoint == storyPoint)
      {
        m_activeStoryPoint = null;
        m_hasActiveStoryPoint = false;
        m_notification.SetActive(false);
      }
    }
  }

  void CreateGrid()
  {
    // Get max amount of people per category
    int totalSlotAmount = DataManager.Instance.GetTotalNumberOfWordsInCategory(m_orbCategory);
    _slots = new Vector3[totalSlotAmount];
    
    // Set length of _expandedOrbs which holds all the objects
    _expandedOrbs = new SmallOrbShaderController[totalSlotAmount];
    _expandedOrbsAnimators = new Animator[totalSlotAmount];

    // Calc amount of rings and amount of orbs per ring
    int amountOfOuterOrbs = totalSlotAmount % m_orbsPerRing;
    int amountOfFullRings = (totalSlotAmount - amountOfOuterOrbs) / m_orbsPerRing;
    
    // Calc division size
    int sliceSize = 360 / m_orbsPerRing;
    int outerSliceSize = 0;
    if (amountOfOuterOrbs > 0)
      outerSliceSize = 360 / amountOfOuterOrbs;

    // Counter for coordinates
    int counter = 0;

    // Value to store starting angle per ring
    float baseAngle = 0;

    // Fill slots with positions for full rings
    for (int i = 0; i < amountOfFullRings; i++)
    {
      baseAngle += ((float)sliceSize/2);
      for (int j = 0; j < m_orbsPerRing; j++)
      {
        float angle = baseAngle + (sliceSize * j);
        float x = Mathf.Sin(Mathf.Deg2Rad * angle) * (m_baseRingRadius + (m_additionalRingRadius * i));
        float y = Mathf.Cos(Mathf.Deg2Rad * angle) * (m_baseRingRadius + (m_additionalRingRadius * i));
        _slots[counter] = new Vector3(x, y,  -.3f*i + zOffsetFromOrb);
        counter++;
      }
    }
    
    // Fill slots with positions for last ring
    for (int j = 0; j < amountOfOuterOrbs; j++)
    {
      float angle = (outerSliceSize * j);
      float randomOffset = Random.Range(0f, .2f);
      float x = Mathf.Sin(Mathf.Deg2Rad * angle) * (m_baseRingRadius + (m_additionalRingRadius * amountOfFullRings) + randomOffset);
      float y = Mathf.Cos(Mathf.Deg2Rad * angle) * (m_baseRingRadius + (m_additionalRingRadius * amountOfFullRings) + randomOffset);
      _slots[counter] = new Vector3(x, y, -.3f*amountOfFullRings);
      counter++;
    }
  }

  private void InstantiateTestOrbs()
  {
    foreach (var slot in _slots)
    {
      GameObject slotVis = Instantiate(m_expandedOrb, transform);
      slotVis.transform.localPosition = slot;
    }
  }

  private void Update()
  {
    //set active of text
    _nameObject.SetActive(IsHovered);
    //reset hovered bool gets set and read later on
    IsHovered = false;

    //Debug testing
    if (Input.GetKeyDown(KeyCode.Space))
      Toggle();

    m_actualTransform.transform.RotateAround(m_mainCamera.transform.position, Vector3.up, m_defaultMoveSpeed * m_currentSpeedModifier * Time.deltaTime);

    // Sin functions
    Vector3 pos = m_actualTransform.transform.position;
    pos.y = Mathf.Sin(m_sinRadValue) * m_sinAmplitude + m_originalHeight;

    m_actualTransform.transform.position = pos;

    m_sinRadValue += m_sinSpeed * Time.deltaTime;

    if (m_sinRadValue >= Mathf.PI)
    {
      m_sinRadValue -= Mathf.PI;
    }

    // End of sin functions

    // rotate text to face camera
    _nameObject.transform.LookAt(m_mainCamera.transform.position);
    _nameObject.transform.Rotate(0, 180, 0);
    m_notification.transform.rotation = _nameObject.transform.rotation;
  }

  private void OnDestroy()
  {
    if (TimeManager.Instance != null)
    {
      TimeManager.Instance.SpeedChangeEvent -= SpeedModified;
    }
  }

  /// <summary>
  /// The function to call when the TimeManager's speed is changed
  /// </summary>
  /// <param name="newModifier"></param>
  private void SpeedModified(float newModifier)
  {
    m_currentSpeedModifier = newModifier;
  }

  /// <summary>
  /// Coroutine to lerp towards in front of the camera
  /// </summary>
  /// <returns></returns>
  private IEnumerator LerpToPlayer()
  {  
    Vector3 flattenedForward = new Vector3(m_mainCamera.transform.forward.x, 0, m_mainCamera.transform.forward.z);
    Vector3 targetPosition = m_mainCamera.transform.position + flattenedForward * m_distanceFromCamera + Vector3.up * 5;
    transform.rotation = Quaternion.Euler(-30, m_mainCamera.transform.eulerAngles.y, 0);

    ExpandChildren();

    while (m_isClicked)
    {
      transform.position = Vector3.Lerp(transform.position, targetPosition, m_lerpSpeed * Mathf.Abs(m_currentSpeedModifier) * Time.deltaTime);
      
      UpdateChildren();

      yield return null;
    }
  }

  /// <summary>
  /// Coroutine to lerp towards its actual position rotating around the player
  /// </summary>
  /// <returns></returns>
  private IEnumerator LerpToActualPosition()
  {
    while (!m_isClicked)
    {
      transform.position = Vector3.Lerp(transform.position, m_actualTransform.transform.position, m_lerpSpeed * Mathf.Abs(m_currentSpeedModifier) * Time.deltaTime);

      yield return null;
    }
  }

  /// <summary>
  /// expands all of the smaller orbs in rings around the current orb
  /// </summary>
  /// <returns></returns>
  private void InitChildren()
  {
    // get list of all orbs needed to be spawned atm
    // instantiate orbs in rings with random extra offsets from the center
    int counter = 0;
    foreach (var dict in DataManager.Instance.GetCategoryData(m_orbCategory))
    {
      GameObject temp = Instantiate(m_expandedOrb, transform);
      temp.GetComponentInChildren<TextMeshPro>().text = dict.Key;
      _expandedOrbs[counter] = temp.GetComponent<SmallOrbShaderController>();
      _expandedOrbs[counter].SetShaderColour(m_orbColour);
      _expandedOrbs[counter].gameObject.transform.localPosition = _slots[counter];

      bool hasStory = m_storyPointsOfCategory.ContainsKey(dict.Key) ? true : false;

      DateTime? startDate = null;
      DateTime? endDate = null;

      if (hasStory)
      {
        StoryPoint storyPoint = m_storyPointsOfCategory[dict.Key];

        startDate = storyPoint.StartDate;
        endDate = storyPoint.EndDate;
      }

      _expandedOrbs[counter].Initialise(m_orbCategory, dict.Key, hasStory, startDate, endDate);
      _expandedOrbsAnimators[counter] = temp.GetComponent<Animator>();
      counter++;
    }

    // trigger animations of start etc
  }

  private void ExpandChildren()
  {
    foreach (var expOrb in _expandedOrbs)
    {
      if (expOrb.transform != transform)
      {
        if (expOrb.transform != _nameObject.transform)
        {
          expOrb.gameObject.SetActive(true);
          expOrb.transform.LookAt(m_mainCamera.transform.position);
        }
      }
    }
  }

  /// <summary>
  /// collapses all of the smaller orbs in rings around the current orb
  /// </summary>
  /// <returns></returns>
  private void CollapseChildren()
  {
    foreach (var expOrb in _expandedOrbs)
    {
      if (expOrb.transform != transform)
      {
        if (expOrb.transform != _nameObject.transform)
        {
          expOrb.gameObject.SetActive(false);
        }
      }
    }
  }
  
  /// <summary>
  /// Update the children 
  /// </summary>
  /// <returns></returns>
  private void UpdateChildren()
  {
    for (int i = 0; i < _expandedOrbs.Length; i++)
    {
      _expandedOrbs[i].gameObject.SetActive(true);
      
      if (DataManager.Instance.GetWordCount(m_orbCategory, _expandedOrbs[i].GetWord(),
            TimeManager.Instance.ConvertCurrentDayToDateTime()) > 0)
      {
        if (!_expandedOrbs[i].m_active)
        {
          //_expandedOrbs[i].gameObject.SetActive(true);
          _expandedOrbsAnimators[i].SetTrigger("SetActive");
          _expandedOrbs[i].m_active = true;
        }
      }
      else
      {
        if (_expandedOrbs[i].m_active)
        {
          //_expandedOrbs[i].gameObject.SetActive(true);
          _expandedOrbsAnimators[i].SetTrigger("SetInActive");
          _expandedOrbs[i].m_active = false;
        }
      }
    }
  }

  public void ActivateStoryPointInterface()
  {
    if (m_hasActiveStoryPoint && m_activeStoryPoint.HasValue)
    {
      m_storyPointInterface.SetInterfaceDetails((StoryPoint)m_activeStoryPoint);
      m_notification.SetActive(false);
    }
  }
}
