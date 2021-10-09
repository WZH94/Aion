public enum Category
{
  Social = 0,
  US_Politics = 1,
  Global_Politics = 2,
  Health = 3,
  Economy = 4
}

public class CategoryExtension
{
  public static string ToString(Category category)
  {
    switch (category)
    {
      case Category.Social:
        return "Social";

      case Category.US_Politics:
        return "US Politics";

      case Category.Global_Politics:
        return "Global Politics";

      case Category.Health:
        return "Health";

      case Category.Economy:
        return "Economy";

      default:
        return "";
    }
  }
}