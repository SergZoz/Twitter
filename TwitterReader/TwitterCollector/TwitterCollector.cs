using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using TwitterDAL;
using CommonDataModels;
using System.Configuration;
using System.Globalization;

namespace TwitterCollector
{
    public partial class TwitterCollector : ServiceBase
    {
        private HttpWebRequest streamRequest;
        public TwitterCollector()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //run long request to twitter stream api
            FollowUser();

            //collect old tweets to DB
            long max_id = GetOldTweets(0);
            while (max_id > 0)
            {
                max_id = GetOldTweets(max_id);
            }
        }

        protected override void OnStop()
        {
            
        }

        private void FollowUser()
        {
            var user_id = ConfigurationManager.AppSettings["twitterUserId"];
            var user_name = ConfigurationManager.AppSettings["twitterUserName"];

            var oauth_consumer_key = ConfigurationManager.AppSettings["oauth_consumer_key"];
            var oauth_consumer_secret = ConfigurationManager.AppSettings["oauth_consumer_secret"];
            var oauth_token = ConfigurationManager.AppSettings["oauth_token"];
            var oauth_token_secret = ConfigurationManager.AppSettings["oauth_token_secret"];
            var oauth_version = ConfigurationManager.AppSettings["oauth_version"];
            var oauth_signature_method = ConfigurationManager.AppSettings["oauth_signature_method"];

            var oauth_nonce = Convert.ToBase64String(
                new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            var timeSpan = DateTime.UtcNow
                - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();

            var resource_url = "https://stream.twitter.com/1.1/statuses/filter.json";
            var follow = user_id;

            var baseFormat = "follow={6}&oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method={2}" +
                            "&oauth_timestamp={3}&oauth_token={4}&oauth_version={5}";

            var baseString = string.Format(baseFormat,
                                        oauth_consumer_key,
                                        oauth_nonce,
                                        oauth_signature_method,
                                        oauth_timestamp,
                                        oauth_token,
                                        oauth_version,
                                         Uri.EscapeDataString(follow)
                                        );

            baseString = string.Concat("GET&", Uri.EscapeDataString(resource_url), "&", Uri.EscapeDataString(baseString));

            var compositeKey = string.Concat(Uri.EscapeDataString(oauth_consumer_secret),
                                    "&", Uri.EscapeDataString(oauth_token_secret));

            string oauth_signature;
            using (HMACSHA1 hasher = new HMACSHA1(ASCIIEncoding.ASCII.GetBytes(compositeKey)))
            {
                oauth_signature = Convert.ToBase64String(
                    hasher.ComputeHash(ASCIIEncoding.ASCII.GetBytes(baseString)));
            }

            // create the request header
            var headerFormat = "OAuth oauth_nonce=\"{0}\", oauth_signature_method=\"{1}\", " +
                               "oauth_timestamp=\"{2}\", oauth_consumer_key=\"{3}\", " +
                               "oauth_token=\"{4}\", oauth_signature=\"{5}\", " +
                               "oauth_version=\"{6}\"";

            var authHeader = string.Format(headerFormat,
                                    Uri.EscapeDataString(oauth_nonce),
                                    Uri.EscapeDataString(oauth_signature_method),
                                    Uri.EscapeDataString(oauth_timestamp),
                                    Uri.EscapeDataString(oauth_consumer_key),
                                    Uri.EscapeDataString(oauth_token),
                                    Uri.EscapeDataString(oauth_signature),
                                    Uri.EscapeDataString(oauth_version)
                            );


            // run request

            var postBody = "follow=" + Uri.EscapeDataString(follow);
            resource_url += "?" + postBody;
            streamRequest = (HttpWebRequest)WebRequest.Create(resource_url);
            streamRequest.Headers.Add("Authorization", authHeader);
            streamRequest.Method = "GET";
            streamRequest.ContentType = "application/x-www-form-urlencoded";

            streamRequest.BeginGetResponse(ar =>
            {
                var req = (WebRequest)ar.AsyncState;
                using (var response = req.EndGetResponse(ar))
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        while (!reader.EndOfStream)
                        {
                            var data = reader.ReadLine();
                            if(!String.IsNullOrEmpty(data))
                            {
                                // new tweet received from Twitter API
                                var newTweet = JsonConvert.DeserializeObject<Tweet>(data);
                                using (var ctx = new TweetContext())
                                {
                                    // save new tweet to database
                                    newTweet.UserName = user_name;
                                    newTweet.UserId = user_id;
                                    DateTime dt = DateTime.ParseExact(newTweet.CreatedAt, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);
                                    newTweet.CreatedAt = dt.ToString("yyyy-MM-dd HH:mm:ss");
                                    ctx.Tweets.Add(newTweet);
                                    ctx.SaveChanges();
                                }
                                // make TwitterReader web-site to refresh all connected users
                                Notify();
                            }
                        }
                    }
                }

            }, streamRequest);

        }

        private long GetOldTweets(long max_id)
        {
            var user_id = ConfigurationManager.AppSettings["twitterUserId"];
            var user_name = ConfigurationManager.AppSettings["twitterUserName"];

            var oauth_consumer_key = ConfigurationManager.AppSettings["oauth_consumer_key"];
            var oauth_consumer_secret = ConfigurationManager.AppSettings["oauth_consumer_secret"];
            var oauth_token = ConfigurationManager.AppSettings["oauth_token"];
            var oauth_token_secret = ConfigurationManager.AppSettings["oauth_token_secret"];
            var oauth_version = ConfigurationManager.AppSettings["oauth_version"];
            var oauth_signature_method = ConfigurationManager.AppSettings["oauth_signature_method"];

            var oauth_nonce = Convert.ToBase64String(
                new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString()));
            var timeSpan = DateTime.UtcNow
                - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var oauth_timestamp = Convert.ToInt64(timeSpan.TotalSeconds).ToString();

            var resource_url = "https://api.twitter.com/1.1/statuses/user_timeline.json";
            var baseFormat = "count={7}{8}&oauth_consumer_key={0}&oauth_nonce={1}&oauth_signature_method={2}" +
                            "&oauth_timestamp={3}&oauth_token={4}&oauth_version={5}&screen_name={6}";

            var baseString = string.Format(baseFormat,
                                        oauth_consumer_key,
                                        oauth_nonce,
                                        oauth_signature_method,
                                        oauth_timestamp,
                                        oauth_token,
                                        oauth_version,
                                         Uri.EscapeDataString(user_name),
                                         200, //max allowed count of tweets in response per one Twitter API request
                                         max_id > 0 ? "&max_id=" + max_id : ""
                                        );

            baseString = string.Concat("GET&", Uri.EscapeDataString(resource_url), "&", Uri.EscapeDataString(baseString));

            var compositeKey = string.Concat(Uri.EscapeDataString(oauth_consumer_secret),
                                    "&", Uri.EscapeDataString(oauth_token_secret));

            string oauth_signature;
            using (HMACSHA1 hasher = new HMACSHA1(ASCIIEncoding.ASCII.GetBytes(compositeKey)))
            {
                oauth_signature = Convert.ToBase64String(
                    hasher.ComputeHash(ASCIIEncoding.ASCII.GetBytes(baseString)));
            }

            // create the request header
            var headerFormat = "OAuth oauth_nonce=\"{0}\", oauth_signature_method=\"{1}\", " +
                               "oauth_timestamp=\"{2}\", oauth_consumer_key=\"{3}\", " +
                               "oauth_token=\"{4}\", oauth_signature=\"{5}\", " +
                               "oauth_version=\"{6}\"";

            var authHeader = string.Format(headerFormat,
                                    Uri.EscapeDataString(oauth_nonce),
                                    Uri.EscapeDataString(oauth_signature_method),
                                    Uri.EscapeDataString(oauth_timestamp),
                                    Uri.EscapeDataString(oauth_consumer_key),
                                    Uri.EscapeDataString(oauth_token),
                                    Uri.EscapeDataString(oauth_signature),
                                    Uri.EscapeDataString(oauth_version)
                            );

            // run request
            var postBody = "screen_name=" + Uri.EscapeDataString(user_name) + String.Format("&count={0}{1}", 200, max_id > 0 ? "&max_id=" + max_id : "");//
            resource_url += "?" + postBody;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(resource_url);
            request.Headers.Add("Authorization", authHeader);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";


            WebResponse response = request.GetResponse();
            string responseData = new StreamReader(response.GetResponseStream()).ReadToEnd();

            //receive bunch of 200 historical tweets
            var tweets = JsonConvert.DeserializeObject<List<Tweet>>(responseData);
            if (tweets.Count == 0)
                return -1;
            using (var ctx = new TweetContext())
            {
                // save historical tweets to db
                foreach (var newTweet in tweets)
                {
                        newTweet.UserName = user_name;
                        newTweet.UserId = user_id;

                        var entity = ctx.Tweets.FirstOrDefault(x => x.Id == newTweet.Id);
                        if (entity == null)
                        {
                            DateTime dt = DateTime.ParseExact(newTweet.CreatedAt, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture);
                            newTweet.CreatedAt = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            ctx.Tweets.Add(newTweet);
                        }
                }
                ctx.SaveChanges();
            }

            return tweets.Min(x => x.Id)-1;
        }

        private void Notify()
        {
            //notify TwitterReader web-site that new tweet was added to DB
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ConfigurationManager.AppSettings["notifyUrl"]);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";

            WebResponse response = request.GetResponse();
            string responseData = new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}
