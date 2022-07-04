﻿using MovieManager.ClassLibrary;
using MovieManager.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MovieManager.BusinessLogic
{
    public class PotPlayerService
    {
        private MovieService _movieService;

        public PotPlayerService(MovieService movieService)
        {
            _movieService = movieService;
        }

        public void BuildPlayList(string title, string path, List<PlayListItem> movies, FileMode fileMode = FileMode.Create)
        {
            try
            {
                var movieLocations = movies.Select(x => x.MovieLocation).ToList();
                var imdbIds = movies.Select(x => x.ImdbId).ToList();
                var fs = new FileStream($"{path}\\{title}.dpl", fileMode);
                using(var writer = new StreamWriter(fs))
                {
                    if(fileMode == FileMode.Create)
                    {
                        var defaultInput = "DAUMPLAYLIST\nplaytime=0\ntopindex=0\nfoldertype=2\nsaveplaypos=0\n";
                        writer.WriteLine(defaultInput);
                    }
                    for (int i = 0; i < movieLocations.Count; i++)
                    {
                        writer.WriteLine($"{i + 1}*file*{movieLocations[i]}");
                    }
                }
                using(var context = new DatabaseContext())
                {
                    foreach(var imdbId in imdbIds)
                    {
                        var movie = context.Movies.Where(x => x.ImdbId == imdbId).FirstOrDefault();
                        if(movie != null)
                        {
                            movie.PlayedCount += 1;
                        }
                        context.SaveChanges();
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
