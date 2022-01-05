using MovieManager.ClassLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieManager.BusinessLogic
{
    public class FileScanner
    {
        private XmlProcessor _xmlEngine;
        private readonly List<string> MovieExtensions = new List<string> { ".avi", ".mp4", ".wmv", ".mkv" };

        public FileScanner(XmlProcessor xmlEngine)
        {
            _xmlEngine = xmlEngine;
        }

        public List<Movie> ScanFiles(string rootDirectory)
        {
            var movies = new List<Movie>();
            try
            {
                var nfos = Directory.GetFiles(rootDirectory, "*.nfo", SearchOption.AllDirectories);
                var allMovies = Directory.GetFiles(rootDirectory, $"*.*", SearchOption.AllDirectories)
                        .Where(f => MovieExtensions.Any(f.ToLower().EndsWith)).ToList();
                movies = ProcessNfos(nfos, allMovies);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return movies;
        }

        private List<Movie> ProcessNfos(string[] nfos, List<string> allMovies)
        {
            var movies = new List<Movie>();
            foreach (var nfo in nfos)
            {
                var movie = _xmlEngine.ParseXmlFile(nfo);
                if (movie != null)
                {
                    var imdb = movie.ImdbId;
                    if (!string.IsNullOrEmpty(movie.ImdbId))
                    {
                        var movieFileLoc = allMovies.Where(x => x.Contains(imdb)).ToList();
                        var sb = new StringBuilder();
                        foreach (var loc in movieFileLoc)
                        {
                            sb.Append(loc + ",");
                            movie.MovieLocation = sb.ToString();
                        }
                        movies.Add(movie);
                    }
                }
            }
            return movies;
        }
    }
}
