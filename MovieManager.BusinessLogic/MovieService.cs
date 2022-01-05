using MovieManager.ClassLibrary;
using MovieManager.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieManager.BusinessLogic
{
    public class MovieService
    {
        public List<MovieViewModel> AllMoviesCache;

        public enum FilterType { Actors, Genres, Tags }
        public enum WildcardType { ImdbId, Title }
        public enum PreciseType { Director, Year, Liked }

        public MovieService()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File($"logs/movieSrv-{DateTime.Now.ToString("yyyyMMddHHmmss")}.txt")
                .CreateLogger();
        }

        public async Task InsertMovies(List<Movie> movies)
        {
            var currentImdb = string.Empty;
            try
            {
                using (var context = new DatabaseContext())
                {
                    var distinctActors = movies.SelectMany(x => x.Actors.Select(x => x)).Distinct().ToList();
                    var distinctGenres = movies.SelectMany(x => x.Genres.Select(x => x)).Distinct().ToList();
                    var distinctTags = movies.SelectMany(x => x.Tags.Select(x => x)).Distinct().ToList();

                    var tasks = new List<Task>();
                    tasks.Add(InsertActors(context, distinctActors));
                    tasks.Add(InsertGenres(context, distinctGenres));
                    tasks.Add(InsertTags(context, distinctTags));
                    await Task.WhenAll(tasks);

                    foreach (var movie in movies)
                    {
                        currentImdb = movie.ImdbId;
                        Log.Debug($"Start to process movie: {movie.ImdbId}.");
                        var exisitingMovie = context.Movies.Where(x => x.ImdbId == movie.ImdbId).FirstOrDefault();
                        if (exisitingMovie == null)
                        {
                            InsertMovie(context, movie);
                        }
                        else if (exisitingMovie != null)
                        {
                            if (!string.IsNullOrEmpty(movie.DateAdded) && !string.IsNullOrEmpty(exisitingMovie.DateAdded))
                            {
                                if (DateTime.Parse(movie.DateAdded) > DateTime.Parse(exisitingMovie.DateAdded))
                                {
                                    UpdateMovie(context, movie, exisitingMovie);
                                }
                            }
                        }
                        context.SaveChanges();
                        Log.Debug($"Movie: {movie.ImdbId} has been added.");
                    }
                    // Remove deleted movies from db
                    List<string> allImdbs = movies.Select(x => x.ImdbId).ToList();
                    var moviesToRemove = context.Movies.Where(x => !allImdbs.Contains(x.ImdbId)).ToList();
                    DeleteMovies(context, moviesToRemove);
                    context.SaveChanges();
                    Log.Debug($"All movies have been processed successfully!");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when processing movie: {currentImdb} \n\r");
                Log.Error(ex.ToString());
            }
        }

        public List<MovieViewModel> GetMovies()
        {
            var result = new List<MovieViewModel>();
            try
            {
                result = new List<MovieViewModel>();
                using (var context = new DatabaseContext())
                {
                    var movies = context.Movies.ToList();
                    foreach (var movie in movies)
                    {
                        MovieViewModel vm = BuildMovieViewModel(context, movie);
                        result.Add(vm);
                    }
                }
                AllMoviesCache = result;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting all movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetMoviesByFilters(FilterType filterType, List<string> filters)
        {
            var result = new List<MovieViewModel>();
            try
            {
                if (AllMoviesCache == null)
                {
                    GetMovies();
                }
                 result = new List<MovieViewModel>();
                using (var context = new DatabaseContext())
                {
                    foreach (var movie in AllMoviesCache)
                    {
                        switch (filterType)
                        {
                            case FilterType.Actors:
                                if (filters.All(x => movie.Actors.Select(x => x.Name).Contains(x)))
                                {
                                    result.Add(movie);
                                }
                                break;
                            case FilterType.Genres:
                                if (filters.All(x => movie.Genres.Select(x => x.Name).Contains(x)))
                                {
                                    result.Add(movie);
                                }
                                break;
                            case FilterType.Tags:
                                if (filters.All(x => movie.Tags.Select(x => x.Name).Contains(x)))
                                {
                                    result.Add(movie);
                                }
                                break;
                        }

                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error($"An error occurs when getting movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetMoviesWildcard(WildcardType wildcardType, string searchString)
        {
            var result = new List<MovieViewModel>();
            var movies = new List<Movie>();
            try
            {
                using (var context = new DatabaseContext())
                {
                    switch (wildcardType)
                    {
                        case WildcardType.ImdbId:
                            movies = context.Movies.Where(x => x.ImdbId.Contains(searchString)).ToList();
                            break;
                        case WildcardType.Title:
                            movies = context.Movies.Where(x => x.Title.Contains(searchString)).ToList();
                            break;
                    }
                    foreach (var m in movies)
                    {
                        result.Add(BuildMovieViewModel(context, m));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetMoviePrecisely(PreciseType preciseType, string searchString)
        {
            var result = new List<MovieViewModel>();
            var movies = new List<Movie>();
            try 
            {
                using (var context = new DatabaseContext())
                {
                    switch (preciseType)
                    {
                        case PreciseType.Director:
                            movies = context.Movies.Where(x => x.Director == searchString).ToList();
                            break;
                        case PreciseType.Liked:
                            var flag = searchString == "Y" ? true : false;
                            movies = context.Movies.Where(x => x.Liked == flag).ToList();
                            break;
                        case PreciseType.Year:
                            var year = int.Parse(searchString);
                            movies = context.Movies.Where(x => x.Year == year).ToList();
                            break;
                    }
                    foreach (var m in movies)
                    {
                        result.Add(BuildMovieViewModel(context, m));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<string> GetMovieLocations(MovieViewModel movie)
        {
            var result = new List<string>();
            try
            {
                if(!string.IsNullOrEmpty(movie.MovieLocation))
                {
                    var locations = movie.MovieLocation.Split(',').ToList();
                    foreach(var loc in locations)
                    {
                        if(!string.IsNullOrEmpty(loc))
                        {
                            result.Add(loc);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Error($"An error occurs when splitting movie locations. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        private MovieViewModel BuildMovieViewModel(DatabaseContext context, Movie movie)
        {
            var actors = context.MovieActors
                            .Where(x => x.ImdbId == movie.ImdbId)
                            .Join(context.Actors,
                                    ma => ma.ActorName,
                                    a => a.Name,
                                    (ma, a) => new ActorViewModel()
                                    {
                                        Name = a.Name,
                                        Age = a.Age,
                                        Height = a.Height,
                                        Liked = a.Liked
                                    }).ToList();
            var genres = context.MovieGenres
                .Where(x => x.ImdbId == movie.ImdbId)
                .Join(context.Genres,
                        mg => mg.GenreName,
                        g => g.Name,
                        (mg, g) => new GenreViewModel()
                        {
                            Name = g.Name
                        }).ToList();
            var tags = context.MovieTags
                .Where(x => x.ImdbId == movie.ImdbId)
                .Join(context.Tags,
                        mt => mt.TagName,
                        t => t.Name,
                        (mt, t) => new TagViewModel()
                        {
                            Name = t.Name
                        }).ToList();
            var vm = new MovieViewModel()
            {
                ImdbId = movie.ImdbId,
                Title = movie.Title,
                Plot = movie.Plot,
                Year = movie.Year,
                Runtime = movie.Runtime,
                Studio = movie.Studio,
                PosterFileLocation = movie.PosterFileLocation,
                FanArtLocation = movie.FanArtLocation,
                MovieLocation = movie.MovieLocation,
                PlayedCount = movie.PlayedCount,
                DateAdded = movie.DateAdded,
                ReleaseDate = movie.ReleaseDate,
                Liked = movie.Liked,
                Genres = genres,
                Tags = tags,
                Actors = actors
            };            
            
            return vm;
        }

        private async Task InsertActors(DatabaseContext context, List<string> actors)
        {
            var allActors = context.Actors.Select(x => x.Name).ToHashSet();

            foreach(var actor in actors)
            {
                if(!allActors.Contains(actor))
                {
                    context.Actors.Add(new Actor()
                    {
                        Name = actor
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private async Task InsertGenres(DatabaseContext context, List<string> genres)
        {
            var allGenres = context.Genres.Select(x => x.Name).ToHashSet();

            foreach (var genre in genres)
            {
                if (!allGenres.Contains(genre))
                {
                    context.Genres.Add(new Genre()
                    {
                        Name = genre
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private async Task InsertTags(DatabaseContext context, List<string> tags)
        {
            var allTags = context.Tags.Select(x => x.Name).ToHashSet();

            foreach (var tag in tags)
            {
                if (!allTags.Contains(tag))
                {
                    context.Tags.Add(new Tag()
                    {
                        Name = tag
                    });
                }
            }
            await context.SaveChangesAsync();
        }

        private void InsertMovie(DatabaseContext context, Movie movie)
        {
            context.Movies.Add(movie);
            InsertForeignKeys(context, movie);
        }

        private void InsertForeignKeys(DatabaseContext context, Movie movie)
        {
            foreach (var actor in movie.Actors)
            {
                context.MovieActors.Add(new MovieActors()
                {
                    ImdbId = movie.ImdbId,
                    ActorName = actor
                });
            }
            foreach (var genre in movie.Genres)
            {
                context.MovieGenres.Add(new MovieGenres()
                {
                    ImdbId = movie.ImdbId,
                    GenreName = genre
                });
            }
            foreach (var tag in movie.Tags)
            {
                context.MovieTags.Add(new MovieTags()
                {
                    ImdbId = movie.ImdbId,
                    TagName = tag
                });
            }
        }

        private void UpdateMovie(DatabaseContext context, Movie movie, Movie exisitingMovie)
        {
            exisitingMovie.ImdbId = movie.ImdbId;
            exisitingMovie.Title = movie.Title;
            exisitingMovie.Plot = movie.Plot;
            exisitingMovie.Year = movie.Year;
            exisitingMovie.Runtime = movie.Runtime;
            exisitingMovie.Studio = movie.Studio;
            exisitingMovie.PosterFileLocation = movie.PosterFileLocation;
            exisitingMovie.FanArtLocation = movie.FanArtLocation;
            exisitingMovie.MovieLocation = movie.MovieLocation;
            exisitingMovie.DateAdded = movie.DateAdded;
            exisitingMovie.ReleaseDate = movie.ReleaseDate;
            exisitingMovie.Genres = movie.Genres;
            exisitingMovie.Tags = movie.Tags;
            exisitingMovie.Actors = movie.Actors;
            exisitingMovie.Director = movie.Director;

            DeleteForeignKeys(context, movie);
            InsertForeignKeys(context, movie);
        }

        private void DeleteMovies(DatabaseContext context, List<Movie> moviesToRemove)
        {
            foreach (var movie in moviesToRemove)
            {
                DeleteForeignKeys(context, movie);
                DeleteFromPlayList(context, movie);
                context.Movies.Remove(movie);
            }
        }

        private void DeleteForeignKeys(DatabaseContext context, Movie movie)
        {
            var existingMovieActors = context.MovieActors.Where(x => x.ImdbId == movie.ImdbId).ToList();
            var exsitingMovieGenres = context.MovieGenres.Where(x => x.ImdbId == movie.ImdbId).ToList();
            var exsitingMovieTags = context.MovieTags.Where(x => x.ImdbId == movie.ImdbId).ToList();

            context.MovieActors.RemoveRange(existingMovieActors);
            context.MovieGenres.RemoveRange(exsitingMovieGenres);
            context.MovieTags.RemoveRange(exsitingMovieTags);
        }

        private void DeleteFromPlayList(DatabaseContext context, Movie movie)
        {
            var playListMovie = context.PlayLists.Where(x => x.ImdbId == movie.ImdbId).ToList();
            context.PlayLists.RemoveRange(playListMovie);
        }
    }
}
