using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BotClient.Models;
using Microsoft.Bot.Connector.DirectLine;

namespace BotClient.Controllers
{
    public class HomeController : Controller
    {
        DLineClient client;
        Conversation objConversation;

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult StartConversation()
        {
            client = new DLineClient();
            objConversation = client.StartBotConversation();
            if (objConversation == null)
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }            
        }

        public JsonResult SendMessage(string inputStr)
        {
            client = new DLineClient();
            string Id = client.SendMessage(inputStr);
            if (string.IsNullOrEmpty(Id))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(true, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}