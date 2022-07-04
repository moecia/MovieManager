using System.ComponentModel.DataAnnotations;

namespace MovieManager.ClassLibrary
{
    public class Actor
    {
        [Key]
        public string Name { get; set; }
        public string DateofBirth { get; set; }
        public string Height { get; set; }
        public string Cup { get; set; }
        public string LastUpdated { get; set; }

        public bool Liked { get; set; }
    }
}
