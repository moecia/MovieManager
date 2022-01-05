using MovieManager.ClassLibrary;
using MovieManager.Data;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MovieManager.Testing
{
    public class DatabaseContextTest
    {
        [Fact]
        public void GetMovies()
        {
            var movies = new List<Movie>();
            using (var context = new DatabaseContext())
            {
                movies = context.Movies.Take(10).ToList();
            }
            movies.Count.ShouldNotBe(0);
        }
    }
}
