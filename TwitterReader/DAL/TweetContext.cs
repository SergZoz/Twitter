using CommonDataModels;
using System.Data.Entity;


namespace TwitterDAL
{
    public class TweetContext : DbContext
    {
        public TweetContext() : base()
        {
            
        }

        public DbSet<Tweet> Tweets { get; set; }
        
    }
}
