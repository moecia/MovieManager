using MovieManager.BusinessLogic;
using System;
using System.Collections.Generic;
using Xunit;

namespace MovieManager.Testing
{
    public class PotPlayerServiceTest
    {
        [Fact]
        public void BuildPotPlayerPlayListTest()
        {
            var movieSrv = new MovieService();
            var potplayerSrv = new PotPlayerService(movieSrv);
            var searchList = new List<string>() { "" };
            var movies = movieSrv.GetMoviesByFilters(MovieService.FilterType.Genres, searchList);
            var path = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\\AppData\\Roaming\\PotPlayerMini64\\Playlist\\";
            potplayerSrv.BuildPlayList("Test", path, movies);
        }
    }
}
