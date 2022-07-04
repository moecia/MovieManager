﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MovieManager.BusinessLogic;
using MovieManager.ClassLibrary;
using MovieManager.Endpoint.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace MovieManager.Endpoint.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MovieController : ControllerBase
    {
        private string badRequestMessage = "Value cannot be null!";
        private string notFoundMessage = "No Movies found!";
        private MovieService _movieService;
        private XmlProcessor _xmlProcessor;
        private IOptions<UserSettings> _config;

        public MovieController(
            MovieService movieService,
            XmlProcessor xmlProcessor,
            IOptions<UserSettings> config)
        {
            _movieService = movieService;
            _xmlProcessor = xmlProcessor;
            _config = config;
        }

        [HttpGet]
        [Route("/movies")]
        public ActionResult Get()
        {
            var movies = _movieService.GetMovies();
            if(movies.Count > 0)
            {
                return Ok(movies);
            }
            return NotFound(notFoundMessage);
        }

        [HttpGet]
        [Route("/movies/recent")]
        public ActionResult GetMostRecentMovies()
        {
            var movies = _movieService.GetMostRecentMovies();
            if (movies.Count > 0)
            {
                return Ok(movies);
            }
            return NotFound(notFoundMessage);
        }

        [HttpGet]
        [Route("/movies/years")]
        public ActionResult GetYears()
        {
            var years = _movieService.GetMovieYears();
            if (years.Count > 0)
            {
                return Ok(years);
            }
            return NotFound(notFoundMessage);
        }

        [HttpPost]
        [Route("/movies/filters")]
        public ActionResult GetMoviesByFilters([FromBody]FilterRequest filterRequest)
        {
            if(filterRequest == null)
            {
                return BadRequest(badRequestMessage);
            }
            var movies = _movieService.GetMoviesByFilters(filterRequest.FilterType, filterRequest.Filters, filterRequest.IsAndOperator);
            if (movies.Count > 0)
            {
                return Ok(movies);
            }
            return NotFound(notFoundMessage);
        }

        [HttpPost]
        [Route("/movies/wildcard")]
        public ActionResult GetMoviesByWildcardSearch([FromBody] WildcardSearchRequest searchRequest)
        {
            if (searchRequest == null)
            {
                return BadRequest(badRequestMessage);
            }
            var movies = _movieService.GetMoviesWildcard(searchRequest.SearchString);
            if (movies.Count > 0)
            {
                return Ok(movies);
            }
            return NotFound(notFoundMessage);
        }

        [HttpGet]
        [Route("/movies/like")]
        public ActionResult GetLikedMovies()
        {
            var movies = _movieService.GetLikedMovies();
            if (movies.Count > 0)
            {
                return Ok(movies);
            }
            return NotFound(notFoundMessage);
        }

        [HttpPut]
        [Route("/movies/like/{imdbId}")]
        public ActionResult LikeMovie(string imdbId)
        {
            if(string.IsNullOrEmpty(imdbId))
            {
                return BadRequest(badRequestMessage);
            }
            return Ok(_movieService.LikeMovie(imdbId));
        }

        [HttpPost]
        [Route("/movies/details")]
        public ActionResult GetMovieDetails([FromBody] MovieViewModel movieViewModel)
        {
            if (movieViewModel == null)
            {
                return BadRequest(badRequestMessage);
            }
            var movie = _movieService.GetMovieDetails(movieViewModel);
            if(movie != null)
            {
                return Ok(movie);
            }
            return NotFound(notFoundMessage);
        }

        [HttpPut]
        [Route("/movies/addnew/{days}")]
        public async Task<ActionResult> AddNewMoviesAsync(int days)
        {
            var scanner = new FileScanner(_xmlProcessor);
            var movieDir = _config.Value.MovieDirectory;
            var m = scanner.ScanFiles(movieDir, days);
            await _movieService.InsertMovies(m);
            return Ok();
        }

        [HttpDelete]
        [Route("/movies/delete")]
        public ActionResult DeleteNotExistMovies()
        {
            var scanner = new FileScanner(_xmlProcessor);
            var movieDir = _config.Value.MovieDirectory;
            var m = scanner.ScanFilesForImdbId(movieDir);
            return Ok(_movieService.DeleteRemovedMovies(m));
        }
    }
}