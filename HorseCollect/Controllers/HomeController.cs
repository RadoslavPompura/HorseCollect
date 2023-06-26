using Newtonsoft.Json;
using BetfairNG;
using HorseCollect.Ctrl;
using HorseCollect.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace HorseCollect.Controllers
{
    public class HomeController : Controller
    {
        BetfairApiCtrl ctrl;

        BetfairAccount bfAccount;
        string bfKey;

        public HomeController()
        {
            string res = string.Empty;

            string username = ConfigurationManager.AppSettings["BetfairAcc_Username"];
            string password = ConfigurationManager.AppSettings["BetfairAcc_Password"];
            bfKey = ConfigurationManager.AppSettings["betfairApiKey"];

            bfAccount = new BetfairAccount(username, password);

            ctrl = new BetfairApiCtrl(bfAccount);
        }
        public ActionResult Index()
        {
            return View();
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

        public string getBfData()
        {
            ctrl.initApi(bfAccount.UserName, bfAccount.Password, bfKey);

            List<HorseApiItem_Bf> resList = ctrl.getApiHorseList();
            if (resList == null)
            {
                doLoginBetfair();
                ctrl.initApi(bfAccount.UserName, bfAccount.Password, bfKey);
            }

            if (resList.Count != 0)
            {
                return JsonConvert.SerializeObject(resList);
            }

            return "Empty";
        }

        private void doLoginBetfair()
        {
            int retryCount = 10;

            ctrl = new BetfairApiCtrl(this.bfAccount);

            //m_betfairCtr.ReadCookiesFromDisk();
            bool bref = ctrl.doLogin(bfAccount.UserName, bfAccount.Password);
            while (!bref && --retryCount > 0)
            {
                Thread.Sleep(1000);
                bref = ctrl.doLogin(bfAccount.UserName, bfAccount.Password);
            }

            if (!bref)
            {
                Console.WriteLine("Betfair login failed...");
                //refreshControls(true);
                return;
            }
            Console.WriteLine("Betfair login successfully...");
        }

    }
}