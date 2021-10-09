using UnityEngine;
using System.Text.RegularExpressions;

// Taken from here and converted: https://bravenewmethod.com/2014/09/13/lightweight-csv-reader-for-unity/
// Comments

public class CSVReader
{
  static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))"; // Define delimiters, regular expression craziness
  static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r"; // Define line delimiters, regular experession craziness
  static char[] TRIM_CHARS = { '\"' };

  /// <summary>
  /// Reads the inputted file and returns them as an array of strings
  /// </summary>
  /// <param name="file"></param>
  /// <returns></returns>
  public static string[] Read( string file ) //Declare method
  {
    float time = Time.realtimeSinceStartup;
    Debug.Log("CSVReader is reading " + file); // Print filename, make sure parsed correctly

    TextAsset data = Resources.Load(file) as TextAsset; //Loads the TextAsset named in the file argument of the function
    if (data == null) return null; //Check that there is more than one line

    Debug.Log("Data loaded:" + data); // Print raw data, make sure parsed correctly

    string[] lines = Regex.Split(data.text, LINE_SPLIT_RE); // Split data.text into lines using LINE_SPLIT_RE characters

    Debug.Log("CSV read, time taken is " + (Time.realtimeSinceStartup - time));

    return lines;
  }
}