namespace MovieManager.ClassLibrary
{
    public enum WildcardType { ImdbId, Title }

    public class WildcardSearchRequest
    {
        public WildcardType WildcardType { get; set; }
        public string SearchString { get; set; }
    }
}
