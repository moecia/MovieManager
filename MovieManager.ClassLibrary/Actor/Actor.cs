using System.ComponentModel.DataAnnotations;

namespace MovieManager.ClassLibrary
{
    public class Actor
    {
        [Key]
        public string Name { get; set; }
        public string Age { get; set; }
        public string Height { get; set; }
        public bool Liked { get; set; }
    }
}
