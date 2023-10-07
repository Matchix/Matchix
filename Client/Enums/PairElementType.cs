public enum ContentType
{
    Image,
    Text
}

public static class ContentTypeExtensions
{
    public static ContentType Random()
    {
        Random rand = new Random();
        return rand.Next(2) == 0 ? ContentType.Image : ContentType.Text;
    }
}