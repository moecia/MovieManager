using MovieManager.ClassLibrary;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MovieManager.BusinessLogic
{
    public class PotPlayerService
    {
        private MovieService _movieService;

        public PotPlayerService(MovieService movieService)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"logs/movieSrv-{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt")
                .CreateLogger();
            _movieService = movieService;
        }

        public void BuildPlayList(string title, string path, List<MovieViewModel> movies)
        {
            try
            {
                var fs = new FileStream($"{path}\\{title}.dpl", FileMode.Create);
                using(var writer = new StreamWriter(fs))
                {
                    var defaultInput = "DAUMPLAYLIST\nplaytime=0\ntopindex=0\nfoldertype=2\nsaveplaypos=0\n";
                    writer.WriteLine(defaultInput);
                    for (int i = 0; i < movies.Count; i++)
                    {
                        var movieLocations = _movieService.GetMovieLocations(movies[i]);
                        foreach(var loc in movieLocations)
                        {
                            writer.WriteLine($"{i + 1}*file*{loc}");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error($"An error occurs when creating potplayer list. \n\r");
                Log.Error(ex.ToString());
            }
        }
    }
}
