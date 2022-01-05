using MovieManager.ClassLibrary;
using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;

namespace MovieManager.BusinessLogic
{
    public class XmlProcessor
    {
        public Movie ParseXmlFile(string xmlFileLocation)
        {
            Movie movie = null;
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFileLocation);
                var imbdId = xmlDoc.GetElementsByTagName("imdbid")[0]?.InnerText ?? xmlDoc.GetElementsByTagName("id")[0]?.InnerText;
                var title = xmlDoc.GetElementsByTagName("title")[0]?.InnerText;
                var plot = xmlDoc.GetElementsByTagName("plot")[0]?.InnerText;
                var year = int.Parse(xmlDoc.GetElementsByTagName("year")[0]?.InnerText);
                var runtime = int.Parse(xmlDoc.GetElementsByTagName("runtime")[0]?.InnerText);
                var studio = xmlDoc.GetElementsByTagName("studio")[0]?.InnerText;
                var posterFileLocation = xmlDoc.GetElementsByTagName("poster")[0]?.InnerText;
                var fanArtFileLocation = xmlDoc.GetElementsByTagName("fanart")[0]?.InnerText;
                var dateAdded = xmlDoc.GetElementsByTagName("dateadded")[0]?.InnerText;
                var releaseDate = xmlDoc.GetElementsByTagName("release")[0]?.InnerText;
                var director = xmlDoc.GetElementsByTagName("director")[0]?.InnerText;
                var genres = GetGenres(xmlDoc.GetElementsByTagName("genre"));
                var tags = GetTags(xmlDoc.GetElementsByTagName("tag"));
                var actors = GetActors(xmlDoc.GetElementsByTagName("actor"));

                movie = new Movie()
                {
                    ImdbId = imbdId,
                    Title = title,
                    Plot = plot,
                    Year = year,
                    Runtime = runtime,
                    Director = director,
                    Studio = studio,
                    PosterFileLocation = posterFileLocation,
                    FanArtLocation = fanArtFileLocation,
                    DateAdded = dateAdded,
                    ReleaseDate = releaseDate,
                    Genres = genres,
                    Tags = tags,
                    Actors = actors
                };
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return movie;
        }

        private List<string> GetGenres(XmlNodeList rawGenres)
        {
            var genres = new List<string>();
            foreach(var rawGenre in rawGenres)
            {
                genres.Add(((XmlNode)rawGenre).InnerText);
            }
            return genres;
        }

        private List<string> GetTags(XmlNodeList rawTags)
        {
            var tags = new List<string>();
            foreach (var rawTag in rawTags)
            {
                tags.Add(((XmlNode)rawTag).InnerText);
            }
            return tags;
        }

        private List<string> GetActors(XmlNodeList rawActors)
        {
            var actors = new List<string>();
            foreach (XmlNode rawActor in rawActors)
            {
                foreach(XmlNode n in rawActor.SelectNodes("name"))
                {
                    actors.Add(n.InnerText);
                }
            }
            return actors;
        }
    }
}
