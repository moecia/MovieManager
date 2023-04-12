using MovieManager.ClassLibrary;
using MovieManager.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using Serilog;
using System.Threading.Tasks;
using System.Threading;

namespace MovieManager.BusinessLogic
{
    public class ScrapeService
    {
        public void GetActorInformation()
        {
            try
            {
                using (var context = new DatabaseContext())
                {
                    var sqlString = $"select * from Actor where LastUpdated IS NULL";
                    var actorNames = context.Database.SqlQuery<Actor>(sqlString).Select(x => x.Name).ToList();

                    var index = 0;
                    foreach (var name in actorNames)
                    {
                        var actor = context.Actors.Where(x => x.Name == name).FirstOrDefault();
                        Log.Debug($"Thread {Thread.CurrentThread.ManagedThreadId}: Start to process {index} : {actor.Name}");
                        var cleanActorName = string.Empty;
                        if(actor.Name.Contains('（'))
                        {
                            cleanActorName = actor.Name.Substring(0, actor.Name.IndexOf('（'));
                        }
                        else
                        {
                            cleanActorName = actor.Name;
                        }
                        var actorInfoPage = GetActorInfoHtml(cleanActorName);
                        if (!string.IsNullOrEmpty(actorInfoPage))
                        {
                            HtmlWeb web = new HtmlWeb();
                            var htmlDoc = web.Load(actorInfoPage);

                            var info = htmlDoc.DocumentNode.SelectNodes("//p").Where(x => x.InnerText.StartsWith(" Born")).FirstOrDefault();
                            var splitInfo = info?.InnerHtml.Split("<br>");
                            if (splitInfo.Length > 0)
                            {
                                var dob = splitInfo.Where(x => x.Trim().StartsWith("Born")).FirstOrDefault()?.Trim();
                                var cup = splitInfo.Where(x => x.Trim().StartsWith("Cup")).FirstOrDefault()?.Trim();
                                var height = info.Descendants(0).Where(x => x.Name == "span").FirstOrDefault()?.InnerText.Trim();
                                if (string.IsNullOrEmpty(dob))
                                {
                                    Log.Error($"Can't find {actor.Name} date of birth information!");
                                }
                                else
                                {
                                    var firstIndex = dob.IndexOf(':');
                                    var dobClean = dob.Substring(firstIndex + 1, dob.Length - firstIndex - 1).Trim();
                                    DateTime temp = new DateTime();
                                    if(DateTime.TryParse(dobClean, out temp))
                                    {
                                        actor.DateofBirth = temp.ToString("yyyy-MM-dd");
                                    }
                                }
                                if (string.IsNullOrEmpty(cup))
                                {
                                    Log.Error($"Can't find {actor.Name} cup information!");
                                }
                                else
                                {
                                    var firstIndex = cup.IndexOf(':');
                                    actor.Cup = cup.Substring(firstIndex + 1, cup.Length - firstIndex - 1).Trim();
                                }
                                if (string.IsNullOrEmpty(height))
                                {
                                    Log.Error($"Can't find {actor.Name} height information!");
                                }
                                else
                                {
                                    actor.Height = height;
                                }
                                actor.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                Log.Error($"Can't find {actor.Name} information!");
                                actor.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
                            }
                            context.SaveChanges();
                        }
                        else
                        {
                            Log.Error($"Can't find {actor.Name} information!");
                            actor.LastUpdated = DateTime.Now.ToString("yyyy-MM-dd");
                            context.SaveChanges();
                        }
                        Log.Debug($"Thread {Thread.CurrentThread.ManagedThreadId}: complete to process {index} : {actor.Name}");
                        index++;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting actor information! \n\r");
                Log.Error(ex.ToString());
            }
        }

        private string GetActorInfoHtml(string actorName)
        {
            var actorInfoPage = string.Empty;

            var queryPage = $@"https://xslist.org/search?query={actorName}";
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(queryPage);
            var nodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var title = node.GetAttributes("title").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(title))
                    {
                        if (title.EndsWith(actorName))
                        {
                            actorInfoPage = node.GetAttributes("href").FirstOrDefault()?.Value;
                            break;
                        }
                    }
                }
            }
            return actorInfoPage;
        }
    }
}
