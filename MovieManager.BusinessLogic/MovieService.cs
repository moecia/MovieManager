﻿using Microsoft.Extensions.Options;
using MovieManager.ClassLibrary;
using MovieManager.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieManager.BusinessLogic
{
    public class MovieService
    {
        private ScrapeService _scrapeService;
        private Dictionary<string, int> diskPortMapping;
        private const int STARTPORT = 8100;

        public MovieService(ScrapeService scrapeService,
            IOptions<UserSettings> config)
        {
            _scrapeService = scrapeService;

            diskPortMapping = new Dictionary<string, int>();
            var currPort = STARTPORT;
            foreach (var l in config.Value?.MovieDirectory.Split(","))
            {
                diskPortMapping.Add(l.Trim().Substring(0, 1), currPort);
                currPort++;
            }
        }

        public async Task InsertMovies(List<Movie> movies, bool scrapeActorInfo = false, bool forceUpdate = false)
        {
            var currentImdb = string.Empty;
            var count = 0;
            try
            {
                using (var context = new DatabaseContext())
                {
                    var distinctActors = movies.SelectMany(x => x.Actors.Select(x => x)).Distinct().ToList();
                    var distinctGenres = movies.SelectMany(x => x.Genres.Select(x => x)).Distinct().ToList();
                    var distinctTags = movies.SelectMany(x => x.Tags.Select(x => x)).Distinct().ToList();

                    var tasks = new List<Task>();
                    tasks.Add(InsertActors(context, distinctActors, scrapeActorInfo));
                    tasks.Add(InsertGenres(context, distinctGenres));
                    tasks.Add(InsertTags(context, distinctTags));
                    await Task.WhenAll(tasks);

                    foreach (var movie in movies)
                    {
                        try
                        {
                            currentImdb = movie.ImdbId;
                            Log.Debug($"Start to process movie: {movie.ImdbId}.");
                            var exisitingMovie = context.Movies.Where(x => x.ImdbId == movie.ImdbId).FirstOrDefault();
                            if (exisitingMovie == null)
                            {
                                InsertMovie(context, movie);
                                count++;
                            }
                            else if (exisitingMovie != null)
                            {
                                //if (exisitingMovie.FanArtLocation == null && exisitingMovie.PosterFileLocation == null)
                                //{
                                //    UpdateMovie(context, movie, exisitingMovie);
                                //}
                                //if (exisitingMovie.MovieLocation != movie.MovieLocation)
                                //{
                                //    UpdateMovie(context, movie, exisitingMovie);
                                //}
                                if (!string.IsNullOrEmpty(movie.DateAdded) && !string.IsNullOrEmpty(exisitingMovie.DateAdded))
                                {
                                    //|| (DateTime.Parse(movie.DateAdded) > DateTime.Parse(exisitingMovie.DateAdded))
                                    if (movie.PosterFileLocation != exisitingMovie.PosterFileLocation
                                        || movie.FanArtLocation != exisitingMovie.FanArtLocation
                                        || movie.MovieLocation != exisitingMovie.MovieLocation
                                        || forceUpdate)
                                    {
                                        UpdateMovie(movie, exisitingMovie);
                                        Log.Information($"Updating {movie.ImdbId} data...");
                                    }
                                }
                            }
                            context.SaveChanges();
                            Log.Debug($"Movie: {movie.ImdbId} has been added.");
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"An error occurs when processing movie: {currentImdb} \n\r");
                            Log.Error(ex.ToString());
                        }
                    }
                    context.SaveChanges();
                    Log.Debug($"{count} movies have been added!");
                    Log.Debug($"All movies have been processed successfully!");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when processing movies. \n\r");
                Log.Error(ex.ToString());
            }
        }

        public List<string> DeleteRemovedMovies(List<string> movies)
        {
            var moviesToRemove = new List<Movie>();
            using(var context = new DatabaseContext())
            {
                moviesToRemove = context.Movies.Where(x => movies.Contains(x.ImdbId) == false).ToList();
                DeleteMovies(moviesToRemove);
            }
            return moviesToRemove.Select(x => x.ImdbId).ToList();
        }

        public List<MovieViewModel> GetMovies()
        {
            var result = new List<MovieViewModel>();
            try
            {
                using (var context = new DatabaseContext())
                {
                    var movies = context.Movies.ToList();
                    result = BuildMovieViewModel(movies);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting all movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetMoviesByFilters(FilterType filterType, List<string> filters, bool isAndOperator)
        {
            var result = new List<MovieViewModel>();
            var movies = new List<Movie>();
            try
            {
                var sb = new StringBuilder();
                var searchString = string.Empty;
                var searchLength = filters.Count;
                var sqlString = string.Empty;
                if (filterType != FilterType.Directors || filterType != FilterType.Years)
                {
                    for (int i = 0; i < filters.Count; ++i)
                    {
                        if (i == filters.Count - 1)
                        {
                            sb.Append($"'{filters[i]}'");
                        }
                        else
                        {
                            sb.Append($"'{filters[i]}',");
                        }
                    }
                    searchString = sb.ToString();
                }

                var yearSearchFilters = new List<int>();
                if(filterType == FilterType.Years)
                {
                    foreach (var yearString in filters)
                    {
                        yearSearchFilters.Add(int.Parse(yearString));
                    }
                }

                using (var context = new DatabaseContext())
                {
                    switch (filterType)
                    {
                        case FilterType.Actors:
                            sqlString = "select * from " +
                            "(select ImdbId, count(ImdbId) as cnt " +
                            $"from MovieActors WHERE ActorName in ({searchString}) group by ImdbId) abc " +
                            $"join Movie m on abc.ImdbId = m.ImdbId where cnt >= {(isAndOperator ? 1 : searchLength).ToString()};";
                            movies = context.Database.SqlQuery<Movie>(sqlString).ToList();
                            break;
                        case FilterType.Genres:
                            sqlString = "select * from " +
                            "(select ImdbId, count(ImdbId) as cnt " +
                            $"from MovieGenres WHERE GenreName in ({searchString}) group by ImdbId) abc " +
                            $"join Movie m on abc.ImdbId = m.ImdbId where cnt >= {(isAndOperator ? 1 : searchLength).ToString()};";
                            movies = context.Database.SqlQuery<Movie>(sqlString).ToList();
                            break;
                        case FilterType.Tags:
                            sqlString = "select * from " +
                            "(select ImdbId, count(ImdbId) as cnt " +
                            $"from MovieTags WHERE TagName in ({searchString}) group by ImdbId) abc " +
                            $"join Movie m on abc.ImdbId = m.ImdbId where cnt >= {(isAndOperator ? 1 : searchLength).ToString()};";
                            movies = context.Database.SqlQuery<Movie>(sqlString).ToList();
                            break;
                        case FilterType.Directors:
                            movies = context.Movies.Where(x => filters.Contains(x.Director)).ToList();
                            break;
                        case FilterType.Years:
                            movies = context.Movies.Where(x => yearSearchFilters.Contains(x.Year)).ToList();
                            break;
                    }
                    result = BuildMovieViewModel(movies);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetMoviesWildcard(string searchString)
        {
            var result = new List<MovieViewModel>();
            try
            {
                using (var context = new DatabaseContext())
                {
                    searchString = searchString.Trim().Replace(' ', '%');
                    var sqlString = $"select * from Movie where Title like '%{searchString}%'";
                    var movies = context.Database.SqlQuery<Movie>(sqlString).ToList();
                    result = BuildMovieViewModel(movies);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetMostRecentMovies()
        {
            var result = new List<MovieViewModel>();
            var movies = new List<Movie>();
            try
            {
                using (var context = new DatabaseContext())
                {
                    //var month = DateTime.Now.AddMonths(-3).Month.ToString();
                    //var year = DateTime.Now.AddMonths(-3).Year.ToString();
                    //var sqlString = $"select * from Movie where DATE(DateAdded) > Date('{year}-{month}-01') order by DateAdded desc";
                    //movies = context.Database.SqlQuery<Movie>(sqlString).ToList();
                    movies = context.Movies.OrderByDescending(x => x.DateAdded).Take(100).ToList();
                    result = BuildMovieViewModel(movies);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<MovieViewModel> GetLikedMovies()
        {
            var result = new List<MovieViewModel>();
            var movies = new List<Movie>();
            try
            {
                using (var context = new DatabaseContext())
                {
                    movies = context.Movies.Where(x => x.Liked).ToList();
                    result = BuildMovieViewModel(movies);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting liked movies. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public List<int> GetMovieYears()
        {
            var result = new List<int>();
            try
            {
                using (var context = new DatabaseContext())
                {
                    result = context.Movies.Select(x => x.Year).Distinct().ToList();
                    result.Sort();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting movie years. \n\r");
                Log.Error(ex.ToString());
            }
            return result;
        }

        public MovieDetails GetMovieDetails(MovieViewModel mvm)
        {
            MovieDetails movieDetails = null;
            using(var context = new DatabaseContext())
            {
                var movie = context.Movies.Where(x => x.ImdbId == mvm.ImdbId).FirstOrDefault();
                var actors = context.MovieActors
                   .Where(x => x.ImdbId == mvm.ImdbId)
                   .Join(context.Actors,
                           ma => ma.ActorName,
                           a => a.Name,
                           (ma, a) => new ActorViewModel()
                           {
                               Name = a.Name,
                               DateofBirth = a.DateofBirth,
                               Height = a.Height,
                               LastUpdated = a.LastUpdated,
                               Cup = a.Cup,
                               Liked = a.Liked
                           }).ToList();
                var genres = context.MovieGenres
                    .Where(x => x.ImdbId == mvm.ImdbId)
                    .Join(context.Genres,
                            mg => mg.GenreName,
                            g => g.Name,
                            (mg, g) => new GenreViewModel()
                            {
                                Name = g.Name
                            }).ToList();
                var tags = context.MovieTags
                    .Where(x => x.ImdbId == mvm.ImdbId)
                    .Join(context.Tags,
                            mt => mt.TagName,
                            t => t.Name,
                            (mt, t) => new TagViewModel()
                            {
                                Name = t.Name
                            }).ToList();
                movieDetails = new MovieDetails()
                {
                    ImdbId = mvm.ImdbId,
                    Title = mvm.Title,
                    Plot = movie.Plot,
                    Year = movie.Year,
                    Runtime = movie.Runtime,
                    Studio = movie.Studio,
                    PosterFileLocation = mvm.PosterFileLocation,
                    FanArtLocation = mvm.FanArtLocation,
                    MovieLocation = mvm.MovieLocation,
                    PlayedCount = movie.PlayedCount,
                    DateAdded = movie.DateAdded,
                    ReleaseDate = movie.ReleaseDate,
                    Liked = movie.Liked,
                    Genres = genres,
                    Tags = tags,
                    Actors = actors
                };
            }
            return movieDetails;

        }

        public bool LikeMovie(string imdbId)
        {
            try
            {
                using(var context = new DatabaseContext())
                {
                    var movie = context.Movies.Where(x => x.ImdbId == imdbId).FirstOrDefault();
                    movie.Liked = !movie.Liked;
                    context.SaveChanges();
                    return movie.Liked;
                }

            }
            catch(Exception ex)
            {
                Log.Error($"An error occurs when setting movie's like flag. \n\r");
                Log.Error(ex.ToString());
            }
            return false;
        }

        private List<MovieViewModel> BuildMovieViewModel(List<Movie> movies)
        {
            var results = new List<MovieViewModel>();
            var lockObject = new object();
            var keyValuePairs = new List<KeyValuePair<Movie, bool>>();
            foreach (var m in movies)
            {
                keyValuePairs.Add(new KeyValuePair<Movie, bool>(m, false));
            }
            var taskArray = new Task[4];
            for (int i = 0; i < taskArray.Length; i++)
            {
                taskArray[i] = Task.Factory.StartNew(() =>
                {
                    for (int j = 0; j < keyValuePairs.Count; j++)
                    {
                        lock (lockObject)
                        {
                            if (!keyValuePairs[j].Value)
                            {
                                var newKvp = new KeyValuePair<Movie, bool>(keyValuePairs[j].Key, true);
                                keyValuePairs[j] = newKvp;
                                var movie = keyValuePairs[j].Key;
                               
                                var movieLocations = movie.MovieLocation.Split('|');
                                var movieClips = new List<MovieViewModel>();
                                for (int k = 0; k < movieLocations.Length; k++)
                                {
                                    if (!string.IsNullOrEmpty(movieLocations[k]))
                                    {
                                        var title = string.Empty;
                                        if (k == 0)
                                        {
                                            title = movie.Title;
                                        }
                                        else
                                        {
                                            title = $"{movie.Title}-cd{k}";
                                        }
                                        movieClips.Add(new MovieViewModel()
                                        {
                                            ImdbId = movie.ImdbId,
                                            Title = title,
                                            PosterFileLocation = GetDiskPort(movie.PosterFileLocation?.Substring(0, 1)) + movie.PosterFileLocation?.Remove(0, 3),
                                            FanArtLocation = GetDiskPort(movie.PosterFileLocation?.Substring(0, 1)) + movie.FanArtLocation?.Remove(0, 3),
                                            MovieLocation = movieLocations[k],
                                            DateAdded = movie.DateAdded
                                        });
                                    }
                                }
                                results.AddRange(movieClips);
                            }
                        }
                    }
                });
            }

            Task.WaitAll(taskArray);
            return results;
        }

        private async Task InsertActors(DatabaseContext context, List<string> actors, bool scrapeActorInfo)
        {
            var allActors = context.Actors.Select(x => x.Name).ToHashSet();

            foreach(var actor in actors)
            {
                if (!allActors.Contains(actor))
                {
                    context.Actors.Add(new Actor()
                    {
                        Name = actor
                    });
                }
            }
            if(scrapeActorInfo)
            {
                _scrapeService.GetActorInformation();
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

        private void UpdateMovie(Movie movie, Movie exisitingMovie)
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

            //DeleteForeignKeys(context, movie);
            //InsertForeignKeys(context, movie);
        }

        private void DeleteMovies(List<Movie> moviesToRemove)
        {
            using(var context = new DatabaseContext())
            {
                foreach (var movie in moviesToRemove)
                {
                    var m = context.Movies.Where(x => x.ImdbId == movie.ImdbId).FirstOrDefault();
                    DeleteForeignKeys(context, m);
                    DeleteFromPlayList(context, m);
                    context.Movies.Remove(m);
                    context.SaveChanges();
                }
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

            context.SaveChanges();
        }

        private void DeleteFromPlayList(DatabaseContext context, Movie movie)
        {
            var playListMovie = context.PlayLists.Where(x => x.ImdbId == movie.ImdbId).ToList();
            context.PlayLists.RemoveRange(playListMovie);
            context.SaveChanges();
        }

        private string GetDiskPort(string disk)
        {
            if(String.IsNullOrEmpty(disk))
            {
                return "";
            }
            return $"http://127.0.0.1:{diskPortMapping[disk]}//";
        }
    }
}
