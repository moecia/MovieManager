using MovieManager.BusinessLogic;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MovieManager.Testing
{
    public class MovieServiceTest
    {
        [Fact]
        public async void MovieServiceProcess_All()
        {
            var fileLocation = @"";
            var xmlEngine = new XmlProcessor();
            var fileScanner = new FileScanner(xmlEngine);
            var movieSvc = new MovieService();
            var movies = fileScanner.ScanFiles(fileLocation);
            await movieSvc.InsertMovies(movies);
        }

        [Fact]
        public void GetMovies()
        {
            var movieSvc = new MovieService();
            var movies = movieSvc.GetMovies();
            movies.Count().ShouldNotBe(0);
        }

        [Fact]
        public void GetMoviesByActors()
        {
            var movieSvc = new MovieService();
            var actors = new List<string>() { "" };
            var movies = movieSvc.GetMoviesByFilters(MovieService.FilterType.Actors, actors);
            movies.Count().ShouldNotBe(0);
        }

        [Fact]
        public void GetMoviesByImdbId()
        {
            var movieSvc = new MovieService();
            var imdbId = "IPX";
            var movies = movieSvc.GetMoviesWildcard(MovieService.WildcardType.ImdbId ,imdbId);
            movies.Count().ShouldNotBe(0);
        }

        [Fact]
        public void GetMoviesByYear()
        {
            var movieSvc = new MovieService();
            var year = "2019";
            var movies = movieSvc.GetMoviePrecisely(MovieService.PreciseType.Year, year);
            movies.Count().ShouldNotBe(0);
        }
    }
}
