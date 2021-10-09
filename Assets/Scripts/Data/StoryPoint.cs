
using UnityEngine.Video;

using System;

public struct StoryPoint
{
  public VideoClip VideoClip;
  public Category Category;
  public string Word;
  public DateTime StartDate, EndDate;
  public string Description;

  public StoryPoint( VideoClip videoClip, Category category, string word, DateTime startDate, DateTime endDate, string description )
  {
    VideoClip = videoClip;
    Category = category;
    Word = word;
    StartDate = startDate;
    EndDate = endDate;
    Description = description;
  }

  public override bool Equals( object obj )
  {
    if (obj.GetType() != typeof(StoryPoint)) return false;

    return this == (StoryPoint)obj;
  }

  public override int GetHashCode()
  {
    return base.GetHashCode();
  }

  public override string ToString()
  {
    return base.ToString();
  }

  public static bool operator ==( StoryPoint l, StoryPoint r )
  {
    return (l.VideoClip == r.VideoClip && l.Category == r.Category && l.Word == r.Word && l.StartDate == r.StartDate && l.EndDate == r.EndDate && l.Description == r.Description);
  }

  public static bool operator !=( StoryPoint l, StoryPoint r )
  {
    return (l.VideoClip != r.VideoClip && l.Category != r.Category && l.Word != r.Word && l.StartDate != r.StartDate && l.EndDate != r.EndDate && l.Description != r.Description);
  }
}
