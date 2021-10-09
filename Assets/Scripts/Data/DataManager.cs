using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

// First key is the category of the all the words, a Category enum. The value of this is another dictionary
// The key of this second dictionary is the tweet word, a string. The value of this is another dictionary
// This third dictionary holds every date, a DateTime, as the key, and number of mentions, an int, of that word on that date
using DataStructure = System.Collections.Generic.Dictionary<Category?, System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<System.DateTime, int>>>;

public class DataManager : Singleton<DataManager>
{
  // Name of the input file, no extension
  //[SerializeField] private string m_inputfile;

  [SerializeField] private string m_socialData, m_usPoliticsData, m_globalPoliticsData, m_healthData, m_EconomyData;

  // Holds all the data from CSV reader
  private DataStructure m_dataStructure = new DataStructure();

  // Cache calculations
  private Dictionary<Category, Dictionary<DateTime, int>> m_totalWordCountByDatePerCategory;
  private Dictionary<Category, Dictionary<DateTime, int>> m_numberOfActiveWordsByDatePerCategory;

  // CSV parsing column information
  private const int DATE_COLUMN = 0;
  private const int WORD_COLUMN = 1;
  private const int COUNT_COLUMN = 2;

  private void Awake()
  {
    DateTime earliestDate = new DateTime(2020, 7, 1);
    DateTime latestDate = new DateTime(2000, 1, 1);

    ParseData(CSVReader.Read(m_socialData), Category.Social, ref earliestDate, ref latestDate);
    ParseData(CSVReader.Read(m_usPoliticsData), Category.US_Politics, ref earliestDate, ref latestDate);
    ParseData(CSVReader.Read(m_globalPoliticsData), Category.Global_Politics, ref earliestDate, ref latestDate);
    ParseData(CSVReader.Read(m_healthData), Category.Health, ref earliestDate, ref latestDate);
    ParseData(CSVReader.Read(m_EconomyData), Category.Economy, ref earliestDate, ref latestDate);

    // Set pointlist to results of function Reader with argument inputfile
    //m_pointList = CSVParser.ReadAndParse(m_inputfile, out DateTime earliestDate, out DateTime latestDate, out m_wordsCategories);

    bool success = TimeManager.Instance.Initialise(earliestDate, latestDate);
    if (success)
    {
      // Cache calculations
      Debug.Log("Ignored?");
      CalculateTotalWordCountPerDatePerCategory();
      CalculateNumberOfActiveWordsPerDatePerCategory();

      StoryPointManager.Instance.Initialise();
      OrbManager.Instance.Initialise(m_dataStructure.Keys.ToArray());
    }
  }

  /// <summary>
  /// Gets all the categories this word belongs to
  /// </summary>
  /// <param name="word"></param>
  /// <returns></returns>
  public IEnumerable<Category> GetCategoriesOfWord( string word )
  {
    List<Category> list = new List<Category>();

    foreach (KeyValuePair<Category?, Dictionary<string, Dictionary<DateTime, int>>> categories in m_dataStructure)
    {
      if (categories.Value.ContainsKey(word))
      {
        list.Add((Category)categories.Key);
      }
    }

    if (list.Count == 0) Debug.LogError("Word " + word + " has no category!");

    return list;
  }

  public IEnumerable<Category?> GetAllCategories()
  {
    return m_dataStructure.Keys.ToList();
  }

  /// <summary>
  /// Gets the word count of the word in a category at a date
  /// </summary>
  /// <param name="category"></param>
  /// <param name="word"></param>
  /// <param name="date"></param>
  /// <returns></returns>
  public int GetWordCount( Category category, string word, int year, int month, int day )
  {
    // Convert the date into the correct format
    DateTime date = new DateTime(year, month, day);

    return GetWordCount(category, word, date);
  }

  /// <summary>
  /// Gets the word count of the word in a category at a date
  /// </summary>
  /// <param name="category"></param>
  /// <param name="word"></param>
  /// <param name="date"></param>
  /// <returns></returns>
  public int GetWordCount( Category category, string word, DateTime date )
  {
    return m_dataStructure[category][word].TryGetValue(date, out int value) ? value : 0;
  }

  ///// <summary>
  ///// Gets the total word count across all the categories this belongs to
  ///// </summary>
  ///// <param name="word"></param>
  ///// <returns></returns>
  //public int GetTotalWordCount( string word )
  //{
  //  int totalCount = 0;

  //  // Find which category this word belongs to
  //  var categories = GetCategoriesOfWord(word);

  //  foreach (Category category in categories)
  //  {
  //    foreach (KeyValuePair<DateTime, int> entry in m_dataStructure[category][word])
  //    {
  //      totalCount += entry.Value;
  //    }
  //  }

  //  return totalCount;
  //}

  /// <summary>
  /// Calculates the total count of every word on every date in every category and store them in a dictionary 
  /// </summary>
  private void CalculateTotalWordCountPerDatePerCategory()
  {
    m_totalWordCountByDatePerCategory = new Dictionary<Category, Dictionary<DateTime, int>>();

    IEnumerable<Category?> categories = GetAllCategories();

    int numberOfDays = TimeManager.Instance.TotalNumDays;

    foreach(Category category in categories)
    {
      m_totalWordCountByDatePerCategory.Add(category, new Dictionary<DateTime, int>());

      for (int i = 1; i <= numberOfDays; ++i)
      {
        DateTime date = TimeManager.Instance.ConvertDayToDateTime(i);
        int wordCount = 0;

        foreach (KeyValuePair<string, Dictionary<DateTime, int>> wordData in m_dataStructure[category])
        {
          wordCount += GetWordCount(category, wordData.Key, date);
        }

        m_totalWordCountByDatePerCategory[category].Add(date, wordCount);
      }
    }
  }

  /// <summary>
  /// Get the total count of every word on a specified data in a category
  /// </summary>
  /// <param name="category"></param>
  /// <param name="date"></param>
  /// <returns></returns>
  public int GetTotalWordCountOnDateInCategory( Category category, DateTime date )
  {
    return m_totalWordCountByDatePerCategory[category][date];
  }

  /// <summary>
  /// Get the word count of the date with the highest word count in a category
  /// ONLY CALL AFTER TIMEMANAGER HAS BEEN INITIALISED
  /// </summary>
  /// <param name="category"></param>
  /// <returns></returns>
  public int GetMaxWordCountInCategory( Category category )
  {
    int maxWordCount = 0;
    int numDays = TimeManager.Instance.TotalNumDays;

    for (int i = 1; i <= numDays; ++i)
    {
      DateTime date = TimeManager.Instance.ConvertDayToDateTime(i);
      int wordCountOfDate = 0;

      foreach (KeyValuePair<string, Dictionary<DateTime, int>> wordData in m_dataStructure[category])
      {
        wordCountOfDate += GetWordCount(category, wordData.Key, date);
      }

      if (wordCountOfDate > maxWordCount)
      {
        maxWordCount = wordCountOfDate;
      }
    }

    return maxWordCount;
  }

  /// <summary>
  /// Get the number of words of the date with the highest number of words in a category
  /// ONLY CALL AFTER TIMEMANAGER HAS BEEN INITIALISED
  /// </summary>
  /// <param name="category"></param>
  /// <returns></returns>
  public int GetMaxNumberOfWordsInCategory( Category category )
  {
    int maxWords = 0;
    int numDays = TimeManager.Instance.TotalNumDays;

    for (int i = 1; i <= numDays; ++i)
    {
      DateTime date = TimeManager.Instance.ConvertDayToDateTime(i);
      int numWords = GetNumberOfActiveWordsInCategory(category, date);

      if (numWords > maxWords)
      {
        maxWords = numWords;
      }
    }

    return maxWords;
  }

  public int GetMaxWordCountOfWordOfCategory( Category category, string word )
  {
    int maxWordCount = 0;
    int numDays = TimeManager.Instance.TotalNumDays;

    foreach (KeyValuePair<DateTime, int> wordCountData in m_dataStructure[category][word])
    {
      int count = wordCountData.Value;

      if (maxWordCount < count)
      {
        maxWordCount = count;
      }
    }

    return maxWordCount;
  }

  public int GetMaxChangeInWordCountOfWordOfCategory( Category category, string word )
  {
    int maxWordCount = 0;
    int numDays = TimeManager.Instance.TotalNumDays;

    List<DateTime> validDatesOfWord = m_dataStructure[category][word].Keys.ToList();

    for (int i = 0; i < validDatesOfWord.Count - 1; ++i)
    {
      int countOnCurrentDate = m_dataStructure[category][word][validDatesOfWord[i]];
      int countOnNextDate = m_dataStructure[category][word][validDatesOfWord[i + 1]];

      int changeInCount = Mathf.Abs(countOnCurrentDate - countOnNextDate);

      if (changeInCount > maxWordCount)
      {
        maxWordCount = changeInCount;
      }
    }

    return maxWordCount;
  }

  /// <summary>
  /// Get the total number of words in a category
  /// </summary>
  /// <param name="category"></param>
  /// <returns></returns>
  public int GetTotalNumberOfWordsInCategory( Category category )
  {
    return m_dataStructure[category].Count;
  }

  /// <summary>
  /// Calculates the number active words on every date in every category and store them in a dictionary 
  /// </summary>
  private void CalculateNumberOfActiveWordsPerDatePerCategory()
  {
    m_numberOfActiveWordsByDatePerCategory = new Dictionary<Category, Dictionary<DateTime, int>>();

    IEnumerable<Category?> categories = GetAllCategories();

    int numberOfDays = TimeManager.Instance.TotalNumDays;

    foreach (Category category in categories)
    {
      m_numberOfActiveWordsByDatePerCategory[category] = new Dictionary<DateTime, int>();

      for (int i = 1; i <= numberOfDays; ++i)
      {
        DateTime date = TimeManager.Instance.ConvertDayToDateTime(i);
        int numberOfWords = 0;

        foreach (KeyValuePair<string, Dictionary<DateTime, int>> wordData in m_dataStructure[category])
        {
          if (GetWordCount(category, wordData.Key, date) > 0)
          {
            ++numberOfWords;
          }
        }

        m_numberOfActiveWordsByDatePerCategory[category].Add(date, numberOfWords);
      }
    }
  }

  /// <summary>
  /// Get the number of words that have a value on a specified date in a category
  /// </summary>
  /// <param name="category"></param>
  /// <param name="date"></param>
  /// <returns></returns>
  public int GetNumberOfActiveWordsInCategory( Category category, DateTime date )
  {
    return m_numberOfActiveWordsByDatePerCategory[category][date];
  }

  public Dictionary<string, Dictionary<DateTime, int>> GetCategoryData( Category category )
  {
    return m_dataStructure[category];
  }

  private void ParseData( string[] lines, Category category, ref DateTime earliestDate, ref DateTime latestDate )
  {
    string[] header = Regex.Split(lines[0], ","); //Split header (element 0)

    float time = Time.realtimeSinceStartup;

    if (!m_dataStructure.ContainsKey(category))
    {
      m_dataStructure.Add(category, new Dictionary<string, Dictionary<DateTime, int>>(StringComparer.OrdinalIgnoreCase));
    }

    // Loops through lines
    for (var i = 1; i < lines.Length; i++)
    {
      string[] values = Regex.Split(lines[i], ","); //Split lines according to SPLIT_RE, store in var (usually string array)
      if (values.Length == 0 || values[0] == "") continue; // Skip to end of loop (continue) if value is 0 length OR first value is empty

      if (header.Length != values.Length)
      {
        // Catch data format errors 
        Debug.LogError("Number of columns does not match number of values in a row! Column length is " + header.Length + ", Number of values is " + values.Length + ", at row " + i);

        continue;
      }

      // Save the data based on the const values and pre-determined data format, non-generic functionality

      string word = values[WORD_COLUMN];
      if (!DateTime.TryParse(values[DATE_COLUMN], Culture.CultureType, DateTimeStyles.None, out DateTime date)) Debug.LogError("Error parsing date into DateTime, string value is " + values[DATE_COLUMN] + ", at row " + i);
      if (!int.TryParse(values[COUNT_COLUMN], out int count)) Debug.LogError("Error parsing word count into int, string value is " + values[COUNT_COLUMN] + ", at row " + i);

      // Check if this word has already been included in the data, if not create the entry
      if (!m_dataStructure[category].ContainsKey(word))
      {
        m_dataStructure[category].Add(word, new Dictionary<DateTime, int>());
      }

      // Check if this date already exists (in case of duplicates)
      if (m_dataStructure[category][word].ContainsKey(date))
      {
        m_dataStructure[category][word][date] += count;
      }

      else
      {
        // The entry for the date and word count
        m_dataStructure[category][word].Add(date, count);
      }

      // Check for earliest and latest dates
      if (DateTime.Compare(date, earliestDate) < 0)
      {
        earliestDate = date;
      }

      if (DateTime.Compare(date, latestDate) > 0)
      {
        latestDate = date;
      }
    }

    Debug.Log("CSV parsed, time taken is " + (Time.realtimeSinceStartup - time));
  }

  private void OnDestroy()
  {
    ResetInstance();
  }
}