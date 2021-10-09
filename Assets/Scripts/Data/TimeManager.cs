using UnityEngine;
using UnityEngine.UI;

using TMPro;

using System;
using System.Collections;

using FMODUnity;

public class TimeManager : Singleton<TimeManager>
{
  [Header("Time Controls Settings")]
  [SerializeField, Range(0.1f, 20f)] private float m_defaultSecondsPerDay = 1f;

  [Header("Time Control Interface Elements")]
  [SerializeField] private TextMeshPro m_dateText;
  [SerializeField] private Button m_playButton;
  [SerializeField] private Button m_pauseButton;
  [SerializeField] private Button m_speedUpButton;
  [SerializeField] private Button m_speedDownButton;
  [SerializeField] private Button m_speedUpButton2x;
  [SerializeField] private Button m_speedDownButton2x;

  [Header("End Of Timeline Settings")]
  [SerializeField, Range(0.1f, 5f)] private float m_blinkInterval = 0.5f;
  [SerializeField, Range(1f, 2f)] private float m_scaleValue = 1.2f;

  // FMOD
  [Header("FMOD Events")]
  [EventRef] public string TimeShiftEvent = "";
  [EventRef] public string PauseEvent = "";
  private FMOD.Studio.EventInstance m_timeShiftState;
  private FMOD.Studio.PARAMETER_ID m_timescrollId, m_scrollSpeedId, m_shiftDirectionId;

  // Timeshift variables
  private int m_timescroll;
  private int m_scrollSpeed;
  private int m_shiftDirection;

  // Checks if any initialisation has already been done and prevents reinitialisation
  private bool m_initialisationPerformed = false;
  // Whether there was an error initialising
  private bool m_errorInitialising = false;
  // Whether the TimeManager is paused and time should stop advancing
  private bool m_paused = false;
  // The timer to advance to the next day
  private float m_advanceDayTimer = 0f;
  // If it has reached the last day
  private bool m_hasReachedEndDate = false;

  // Representation of the start and end dates in DateTime format. Used when converting from days to date format
  private DateTime m_startDate;
  private DateTime m_endDate;

  // The number of days from the beginning of the century from the start date
  private int m_startDayNum;
  // The number of days from the beginning of the century from the end date
  public int TotalNumDays { get; private set; } = int.MaxValue;

  private int m_speedModifier = 1;

  // The current day within the database timeframe
  private int m_currentDay;
  public int CurrentDay
  {
    get { return m_currentDay; }
    private set { 
      // Day starts at day 1 not day 0
      m_currentDay = Mathf.Clamp(value, 1, TotalNumDays);
      // When the day is changed, update the text as well
      m_dateText.text = "Day " + m_currentDay.ToString() + ": " + ConvertCurrentDayToDateTimeString();
    }
  }

  public Action<DateTime, bool> TimeParameterChangeEvent;
  public Action<float> SpeedChangeEvent;
  public Action<DateTime, DateTime> DateChangeEvent;
  public Action<float> TimeAdvanceEvent;
  public Action<bool> EndOfTimelineEvent;

  private void Start()
  {
    // Should be called after Initialise and after Orbs are created
    DateTime currentDate = ConvertCurrentDayToDateTime();
    DateChangeEvent?.Invoke(currentDate, ConvertDayToDateTime(m_speedModifier > 0 ? CurrentDay + 1 : CurrentDay - 1));
    SpeedChangeEvent?.Invoke(m_speedModifier);
    TimeParameterChangeEvent?.Invoke(currentDate, m_speedModifier > 0);

    // FMOD
    m_timeShiftState = RuntimeManager.CreateInstance(TimeShiftEvent);

    FMOD.Studio.EventDescription timeShiftEventDescription;
    m_timeShiftState.getDescription(out timeShiftEventDescription);
    FMOD.Studio.PARAMETER_DESCRIPTION timescrollDescription;
    FMOD.Studio.PARAMETER_DESCRIPTION scrollSpeedDescription;
    FMOD.Studio.PARAMETER_DESCRIPTION shiftDirectionDescription;

    timeShiftEventDescription.getParameterDescriptionByName("ScrollSpeed", out scrollSpeedDescription);
    timeShiftEventDescription.getParameterDescriptionByName("ShiftDirection", out shiftDirectionDescription);
    timeShiftEventDescription.getParameterDescriptionByName("Timescroll", out timescrollDescription);

    m_scrollSpeedId = scrollSpeedDescription.id;
    m_shiftDirectionId = shiftDirectionDescription.id;
    m_timescrollId = timescrollDescription.id;
  }

  private void Update()
  {
    if (!m_paused && !m_errorInitialising) AdvanceTime();
  }

  public bool Initialise(DateTime earliestDate, DateTime latestDate)
  {
    if (m_initialisationPerformed) return false;

    m_startDate = earliestDate;
    m_endDate = latestDate;

    m_startDayNum = ConvertTicksToDays(m_startDate.Ticks);
    int endDayNum = ConvertTicksToDays(m_endDate.Ticks);

    if (endDayNum < m_startDayNum)
    {
      string errorMessage = "The end date is earlier than the start date! Start date is " + new DateTime(m_startDate.Ticks).ToString("dd MMMM yyyy") + ", end date is " + new DateTime(m_endDate.Ticks).ToString("dd MMMM yyyy");
      Debug.LogError(errorMessage);
      DisplayError(errorMessage);

      return false;
    }

    // + 1 to include the start date
    TotalNumDays = endDayNum - m_startDayNum + 1;

    // Possible error parsing date data from csv
    if (TotalNumDays <= 1)
    {
      string errorMessage = "There are not enough days in the database to be valid! Number of days: " + TotalNumDays;
      Debug.LogError(errorMessage);
      DisplayError(errorMessage);

      return false;
    }

    else if (TotalNumDays > 365)
    {
      string errorMessage = "INVALID PARSING OF DATE, NUMBER OF DAYS IS " + TotalNumDays;
      Debug.LogError(errorMessage);
      DisplayError(errorMessage);

      return false;
    }

    // Default start at day 1
    CurrentDay = 1;

    m_initialisationPerformed = true;

    return true;
  }

  public int ConvertTicksToDays(long ticks)
  {
    TimeSpan timeSpan = new TimeSpan(ticks);

    return timeSpan.Days;
  }

  public string ConvertCurrentDayToDateTimeString()
  {
    int numDaysFromStartDate = CurrentDay - 1;
    long ticksOfCurrentDay = m_startDate.Ticks + TimeSpan.TicksPerDay * numDaysFromStartDate;

    return new DateTime(ticksOfCurrentDay).ToString("dd MMMM yyyy", Culture.CultureType);
  }

  public DateTime ConvertCurrentDayToDateTime()
  {
    int numDaysFromStartDate = CurrentDay - 1;
    long ticksOfCurrentDay = m_startDate.Ticks + TimeSpan.TicksPerDay * numDaysFromStartDate;

    return new DateTime(ticksOfCurrentDay);
  }

  public DateTime ConvertDayToDateTime(int day)
  {
    int numDaysFromStartDate = day - 1;
    long ticksOfCurrentDay = m_startDate.Ticks + TimeSpan.TicksPerDay * numDaysFromStartDate;

    return new DateTime(ticksOfCurrentDay);
  }

  public void TogglePause()
  {
    PauseTimeShift(m_paused, !m_paused);

    m_paused = !m_paused;

    if (m_paused) FMODUnity.RuntimeManager.PlayOneShot(PauseEvent);

    m_playButton.gameObject.SetActive(m_paused);
    m_pauseButton.gameObject.SetActive(!m_paused);
    
    PauseInvoke();
  }

  public void SetPause(bool pause)
  {
    PauseTimeShift(m_paused, pause);

    if (m_paused != pause && pause) FMODUnity.RuntimeManager.PlayOneShot(PauseEvent);

    m_paused = pause;

    m_playButton.gameObject.SetActive(m_paused);
    m_pauseButton.gameObject.SetActive(!m_paused);

    PauseInvoke();
  }

  private void PauseTimeShift(bool oldValue, bool newValue)
  {
    if (oldValue != newValue && !newValue)
    {
      m_scrollSpeed = 1;
      m_shiftDirection = 1;
      m_timescroll = m_speedModifier > 0 ? 1 : -1;

      TriggerTimeShift();
    }
  }

  private void TriggerTimeShift()
  {
    if (m_timescroll != 0 && m_scrollSpeed > 0)
    {
      m_timeShiftState.setParameterByID(m_timescrollId, m_timescroll);
      m_timeShiftState.setParameterByID(m_scrollSpeedId, m_scrollSpeed);
      m_timeShiftState.setParameterByID(m_shiftDirectionId, m_shiftDirection);
      m_timeShiftState.start();
    }
  }

  private void PauseInvoke()
  {
    float speedToSend = m_paused ? 0f : m_speedModifier;

    SpeedChangeEvent?.Invoke(speedToSend);
  }

  public void ResetDay()
  {
    CurrentDay = 1;

    m_advanceDayTimer = 0f;

    CheckIfEndOfTimeline();
  }

  public void ChangeSpeed(bool faster)
  {
    SetPause(false);
    
    switch (m_speedModifier)
    {
      case -2:
        m_speedModifier = faster ? -1 : -2;

        m_timescroll = faster ? -1 : 0;
        m_scrollSpeed = faster ? 2 : 0;
        m_shiftDirection = faster ? -1 : 0;
        break;

      case -1:
        m_speedModifier = faster ? 1 : -2;

        m_timescroll = faster ? 1 : -1;
        m_scrollSpeed = faster ? 1 : 2;
        m_shiftDirection = faster ? 0 : 1;
        break;

      case 1:
        m_speedModifier = faster ? 2 : -1;

        m_timescroll = faster ? 1 : -1;
        m_scrollSpeed = faster ? 2 : 1;
        m_shiftDirection = faster ? 1 : 0;
        break;

      case 2:
        m_speedModifier = faster ? 2 : 1;

        m_timescroll = faster ? 0 : 1;
        m_scrollSpeed = faster ? 0 : 2;
        m_shiftDirection = faster ? 0 : -1;
        break;
    }

    switch (m_speedModifier)
    {
      case -2:
        m_speedUpButton.gameObject.SetActive(true);
        m_speedUpButton2x.gameObject.SetActive(false);
        m_speedDownButton.gameObject.SetActive(false);
        m_speedDownButton2x.gameObject.SetActive(true);
        break;
      case -1:
        m_speedUpButton.gameObject.SetActive(true);
        m_speedUpButton2x.gameObject.SetActive(false);
        m_speedDownButton.gameObject.SetActive(true);
        m_speedDownButton2x.gameObject.SetActive(false);
        break;
      case 1:
        m_speedUpButton.gameObject.SetActive(true);
        m_speedUpButton2x.gameObject.SetActive(false);
        m_speedDownButton.gameObject.SetActive(true);
        m_speedDownButton2x.gameObject.SetActive(false);
        break;
      case 2:
        m_speedUpButton.gameObject.SetActive(false);
        m_speedUpButton2x.gameObject.SetActive(true);
        m_speedDownButton.gameObject.SetActive(true);
        m_speedDownButton2x.gameObject.SetActive(false);
        break;
    }

    TriggerTimeShift();

    bool endOfTimeline = CheckIfEndOfTimeline();
    DateTime currentDate = ConvertCurrentDayToDateTime();

    if (!endOfTimeline)
    {
      SpeedChangeEvent?.Invoke(m_speedModifier);
      TimeParameterChangeEvent?.Invoke(currentDate, m_speedModifier > 0);
    }
  }

  private void DisplayError(string errorMessage)
  {
    m_errorInitialising = true;
    m_dateText.text = errorMessage;
    m_dateText.color = Color.red;
  }

  private void AdvanceTime()
  {
    m_advanceDayTimer += Time.deltaTime * m_speedModifier;

    if (m_advanceDayTimer >= m_defaultSecondsPerDay || m_advanceDayTimer < 0)
    {
      // Advance day first, to check the end of the timeline. Do not reset timer yet as the timer value is needed to know if it has reached the start of the first day if going backwards
      CurrentDay += m_speedModifier > 0 ? 1 : -1;

      // Check if the timer should be able to advance anymore
      bool endOfTimeline = CheckIfEndOfTimeline();

      // Reset timer here only if not at end of timeline since that already resets the timer, and only after checking the timer value
      if (!endOfTimeline)
      {
        m_advanceDayTimer += m_speedModifier > 0 ? -m_defaultSecondsPerDay : m_defaultSecondsPerDay;
      }
      
      DateTime currentDate = ConvertCurrentDayToDateTime();

      TimeParameterChangeEvent?.Invoke(currentDate, m_speedModifier > 0);
      DateChangeEvent?.Invoke(currentDate, endOfTimeline ? currentDate : ConvertDayToDateTime(m_speedModifier > 0 ? CurrentDay + 1 : CurrentDay - 1));
    }

    TimeAdvanceEvent?.Invoke(m_advanceDayTimer / m_defaultSecondsPerDay);
  }

  private bool CheckIfEndOfTimeline()
  {
    // Check if at the last day when speed is positive OR if at the first day when speed is negative
    // When going backwards, also check the timer value since it has to go back to 0 before it counts as the true start of the timeline
    if ((m_speedModifier > 0 && m_currentDay >= TotalNumDays) || (m_speedModifier < 0 && m_currentDay <= 1 && m_advanceDayTimer <= 0))
    {
      SetPause(true);

      //EndOfTimelineEvent?.Invoke(m_zpeedModifier > 0);
      if (m_speedModifier > 0) StartCoroutine(BlinkPlayButton());

      m_speedModifier = m_speedModifier > 0 ? -1 : 1;
      m_advanceDayTimer = 0f;

      return true;
    }

    return false;
  }

  private void OnDestroy()
  {
    ResetInstance(); 
  }

  private IEnumerator BlinkPlayButton()
  {
    Vector3 originalScale = m_playButton.transform.localScale;
    m_playButton.transform.localScale = originalScale * m_scaleValue;

    Image buttonImage = m_playButton.GetComponentsInChildren<Image>()[1];

    // So the first blink enables it
    buttonImage.enabled = false;

    while (m_paused == true)
    {
      buttonImage.enabled = !buttonImage.enabled;

      yield return new WaitForSeconds(m_blinkInterval);
    }

    buttonImage.enabled = true;
    m_playButton.transform.localScale = originalScale;
  }
}
