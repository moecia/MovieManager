using MovieManager.ClassLibrary;
using MovieManager.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MovieManager.BusinessLogic
{
    public class ActorService
    {
        public ActorService()
        {
            CultureInfo PronoCi = new CultureInfo(2052);
        }

        public List<ActorViewModel> GetAll()
        {
            var results = new List<ActorViewModel>();
            try
            {
                using(var dbContext = new DatabaseContext())
                {
                    var actors = dbContext.Actors.ToList();
                    actors.Sort(delegate (Actor x, Actor y) {
                        return x.Name.CompareTo(y.Name);
                    });
                    results = BuildActorViewModels(actors);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"An error occurs when getting actors. \n\r");
                Log.Error(ex.ToString());
            }
            return results;
        }

        public List<string> GetAllNames()
        {
            var results = new List<string>();
            try
            {
                using (var dbContext = new DatabaseContext())
                {
                    results = dbContext.Actors.Select(x => x.Name).ToList();
                    results.Sort();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting actor names. \n\r");
                Log.Error(ex.ToString());
            }
            return results;
        }

        public List<ActorViewModel> GetByName(string searchString)
        {
            var results = new List<ActorViewModel>();
            try
            {
                using (var dbContext = new DatabaseContext())
                {
                    var sqlString = @$"select * from Actor where Name like '%{searchString}%'";
                    var actors = dbContext.Database.SqlQuery<Actor>(sqlString).ToList();
                    actors.Sort(delegate (Actor x, Actor y) {
                        return x.Name.CompareTo(y.Name);
                    });
                    results = BuildActorViewModels(actors);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting actor. \n\r");
                Log.Error(ex.ToString());
            }
            return results;
        }

        public List<ActorViewModel> GetByRange(int heightLower, int heightUpper, string cupLower, string cupUpper, int age)
        {
            var results = new List<ActorViewModel>();
            var sqlString = "select * from Actor " +
                $"where Height between '{heightLower}' and '{heightUpper}' " +
                $"and Cup between '{cupLower} Cup' and '{cupUpper} Cup' " +
                $"and date(DateOfBirth, '+{age} years') >= date('now') order by Height desc;";
            try
            {
                using (var context = new DatabaseContext())
                {
                    var actors = context.Database.SqlQuery<Actor>(sqlString).ToList();
                    actors.Sort(delegate (Actor x, Actor y) {
                        return x.Name.CompareTo(y.Name);
                    });
                    results = BuildActorViewModels(actors);
                }
            }
            catch(Exception ex)
            {
                Log.Error($"An error occurs when getting actors by range. \n\r");
                Log.Error(ex.ToString());
            }
            return results;
        }

        public List<ActorViewModel> GetLikedActors()
        {
            var results = new List<ActorViewModel>();
            try
            {
                using (var dbContext = new DatabaseContext())
                {
                    var actors = dbContext.Actors.Where(x => x.Liked).ToList();
                    results = BuildActorViewModels(actors);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when getting liked actor. \n\r");
                Log.Error(ex.ToString());
            }
            return results;
        }

        public bool LikeActor(string actorName)
        {
            try
            {
                using (var context = new DatabaseContext())
                {
                    var actor = context.Actors.Where(x => x.Name == actorName).FirstOrDefault();
                    actor.Liked = !actor.Liked;
                    context.SaveChanges();
                    return actor.Liked;
                }

            }
            catch (Exception ex)
            {
                Log.Error($"An error occurs when setting actor's like flag. \n\r");
                Log.Error(ex.ToString());
            }
            return false;
        }

        private List<ActorViewModel> BuildActorViewModels(List<Actor> actors)
        {
            var result = new List<ActorViewModel>();
            var lockObject = new object();
            var keyValuePairs = new List<KeyValuePair<Actor, bool>>();
            foreach(var a in actors)
            {
                keyValuePairs.Add(new KeyValuePair<Actor, bool>(a, false));
            }
            var taskArray = new Task[8];
            for (int i = 0; i < taskArray.Length; i++)
            {
                taskArray[i] = Task.Factory.StartNew(() =>
                {
                    for (int j = 0; j < keyValuePairs.Count; j++)
                    {
                        lock(lockObject)
                        {
                            if (!keyValuePairs[j].Value)
                            {
                                var newKvp = new KeyValuePair<Actor, bool>(keyValuePairs[j].Key, true);
                                keyValuePairs[j] = newKvp;
                                var actor = keyValuePairs[j].Key;
                                result.Add(new ActorViewModel()
                                {
                                    Cup = actor.Cup,
                                    DateofBirth = actor.DateofBirth,
                                    Height = actor.Height,
                                    LastUpdated = actor.LastUpdated,
                                    Liked = actor.Liked,
                                    Name = actor.Name
                                });
                            }
                        }
                    }
                });
            }
            Task.WaitAll(taskArray);
            return result;
        }

        private List<ActorViewModel> BuildActorViewModels_SingleThread(List<Actor> actors)
        {
            var result = new List<ActorViewModel>();
            foreach (var actor in actors)
            {
                result.Add(new ActorViewModel()
                {
                    Cup = actor.Cup,
                    DateofBirth = actor.DateofBirth,
                    Height = actor.Height,
                    LastUpdated = actor.LastUpdated,
                    Liked = actor.Liked,
                    Name = actor.Name
                });
            }
            return result;
        }
    }
}
