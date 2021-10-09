using UnityEngine;
using UnityEngine.Video;

using System;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

public class StoryPointManager : Singleton<StoryPointManager>
{
  [SerializeField] private string m_filename;
  private List<StoryPoint> m_storyPoints = new List<StoryPoint>();
  private Dictionary<Category, Dictionary<string, StoryPoint>> m_storyPointsCategories;

  private bool m_isInitialised = false;

  // CSV parsing column information
  private const int FILENAME_COLUMN = 0;
  private const int CATEGORY_COLUMN = 1;
  private const int WORD_COLUMN = 2;
  private const int START_DATE_COLUMN = 3;
  private const int END_DATE_COLUMN = 4;
  private const int DESCRIPTION_COLUMN = 5;

  [FMODUnity.EventRef]
  public string StoryPointStartTriggerEvent = "";

  public void Initialise()
  {
    if (!m_isInitialised)
    {
      m_isInitialised = true;

      ParseData(CSVReader.Read("Story Points/" + m_filename));
      SortStoryPointsIntoCategoryAndWords();

      TimeManager.Instance.TimeParameterChangeEvent += OnDateChanged;
    } 
  }

  private void SortStoryPointsIntoCategoryAndWords()
  {
    m_storyPointsCategories = new Dictionary<Category, Dictionary<string, StoryPoint>>();

    foreach (StoryPoint storyPoint in m_storyPoints)
    {
      Category category = storyPoint.Category;

      if (!m_storyPointsCategories.ContainsKey(category))
      {
        m_storyPointsCategories.Add(category, new Dictionary<string, StoryPoint>(StringComparer.OrdinalIgnoreCase));
      }

      m_storyPointsCategories[category].Add(storyPoint.Word, storyPoint);
    }
  }

  private void OnDateChanged( DateTime newDate, bool forward )
  {
    for (int i = 0; i < m_storyPoints.Count; ++i)
    {
      DateTime startDate = m_storyPoints[i].StartDate;
      DateTime endDate = m_storyPoints[i].EndDate;

      // Create or remove the trigger at storypoint's category orb, need a reference to the orb
      if (DateTime.Compare(startDate, newDate) == 0)
      {
        if (forward)
        {
          FMODUnity.RuntimeManager.PlayOneShot(StoryPointStartTriggerEvent, OrbManager.Instance.GetOrbWithCategory(m_storyPoints[i].Category).transform.position);
          OrbManager.Instance.SetOrbStoryPoint(m_storyPoints[i], true);
          Debug.Log("Start StoryPoint triggered! " + newDate.ToString("dd MMMM yyyy"));
        }

        else
        {
          OrbManager.Instance.SetOrbStoryPoint(m_storyPoints[i], false);
          Debug.Log("End StoryPoint triggered! " + newDate.ToString("dd MMMM yyyy"));
        }
      }

      else if (DateTime.Compare(endDate, newDate) == 0)
      {
        if (forward)
        {
          OrbManager.Instance.SetOrbStoryPoint(m_storyPoints[i], false);
          Debug.Log("End StoryPoint triggered! " + newDate.ToString("dd MMMM yyyy"));
        }

        else
        {
          FMODUnity.RuntimeManager.PlayOneShot(StoryPointStartTriggerEvent, OrbManager.Instance.GetOrbWithCategory(m_storyPoints[i].Category).transform.position);
          OrbManager.Instance.SetOrbStoryPoint(m_storyPoints[i], true);
          Debug.Log("Start StoryPoint triggered! " + newDate.ToString("dd MMMM yyyy"));
        }
      }
    }
  }

  private void ParseData( string[] lines )
  {
    string[] header = Regex.Split(lines[0], ","); //Split header (element 0)

    // Loops through lines
    for (var i = 1; i < lines.Length; i++)
    {
      string[] values = Regex.Split(lines[i], ","); //Split lines according to SPLIT_RE, store in var (usually string array)
      if (values.Length == 0) Debug.LogError("Error reading values of Story Point!");

      // Save video clip
      string filename = values[FILENAME_COLUMN];
      VideoClip videoClip = Resources.Load("Story Points/Videos/" + filename) as VideoClip;

      // Save categoruy
      Category category = (Category)ConvertStringToCategory(values[CATEGORY_COLUMN]);

      // Save word
      string word = values[WORD_COLUMN];

      // Save the data based on the const values and pre-determined data format, non-generic functionality
      if (!DateTime.TryParse(values[START_DATE_COLUMN], Culture.CultureType, DateTimeStyles.None, out DateTime startDate)) Debug.LogError("Error parsing date into DateTime, string value is " + values[START_DATE_COLUMN]);
      if (!DateTime.TryParse(values[END_DATE_COLUMN], Culture.CultureType, DateTimeStyles.None, out DateTime endDate)) Debug.LogError("Error parsing date into DateTime, string value is " + values[END_DATE_COLUMN]);

      // Check if the dates are correct
      if (DateTime.Compare(startDate, endDate) > 0) Debug.LogError("Start date of Story Point is later than the end date! Start date: " + startDate.ToString("dd MMMM yyyy") + ", end date: " + endDate.ToString("dd MMMM yyyy"));
      if (DateTime.Compare(startDate, endDate) == 0) Debug.LogError("Start date of Story Point is equal to the end date! Start date: " + startDate.ToString("dd MMMM yyyy") + ", end date: " + endDate.ToString("dd MMMM yyyy"));

      // This may happen as the description has commas in it, append all the values together and add a comma behind each value
      string description = "";

      if (values.Length > DESCRIPTION_COLUMN + 1)
      {
        for (int sentenceIndex = DESCRIPTION_COLUMN; sentenceIndex < values.Length; ++sentenceIndex)
        {
          description += values[sentenceIndex] + ",";
        }

        // Removes the last comma
        description = description.Remove(description.Length - 1, 1);
      }

      else description = values[DESCRIPTION_COLUMN];

      // Create the StoryPoint and assign the values
      m_storyPoints.Add(new StoryPoint(videoClip, category, word, startDate, endDate, description));
    }
  }

  private Category? ConvertStringToCategory(string category)
  {
    switch (category)
    {
      case "social":
        return Category.Social;

      case "us":
        return Category.US_Politics;

      case "global":
        return Category.Global_Politics;

      case "health":
        return Category.Health;

      case "economy":
        return Category.Economy;

      default:
        Debug.LogError("Invalid category found in storypoint! Category is " + category);
        return null;
    }
  }

  private void OnDestroy()
  {
    if (TimeManager.Instance != null)
    {
      TimeManager.Instance.TimeParameterChangeEvent -= OnDateChanged;
    }

    ResetInstance();
  }

  public Dictionary<string, StoryPoint> GetStoryPointsByCategory(Category category)
  {
    return m_storyPointsCategories.TryGetValue(category, out Dictionary<string, StoryPoint> value) ? value : new Dictionary<string, StoryPoint>();
  }
}
