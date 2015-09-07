using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CommonDataModels;
using TwitterDAL;
using PagedList;

namespace TwitterReader.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Twitter");
        }
        public ActionResult Twitter(string currentFilter, string searchString, int? page)
        {
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            ViewBag.Message = "Twitter page.";

            var ctx = new TweetContext();
            
            IQueryable<Tweet> twts = ctx.Tweets.OrderByDescending(x=>x.Id);

            if (!String.IsNullOrEmpty(searchString))
            {
                var words = searchString.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLower()).ToArray();
                var results = twts.Select(x => x)
                   .Where(x => words.Any(y => x.Message.ToLower().Contains(y)));

                twts = results;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View("Tweets", twts.ToPagedList(pageNumber, pageSize));
        }


        public ActionResult Notify()
        {
            TwitterReader.Hubs.TwitterHub.Static_Send();
            return new EmptyResult();
        }
    }
}