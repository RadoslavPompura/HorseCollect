using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using HtmlAgilityPack;
using BetfairNG;
using BetfairNG.Data;
using System.Configuration;
using HorseCollect.Json;
using HorseScraper.Json;

namespace HorseCollect.Ctrl
{
    class BetfairApiCtrl
    {
        public string _applicationKey = "L5nPofJuFzU7AUiV";
        //protected onWriteStatusEvent m_handlerWriteStatus;
        //protected onWriteLogEvent m_handlerWriteLog;
        protected CookieContainer m_cookieContainer;
        protected BetfairAccount m_account;
        protected HttpClient m_httpClient = null;
        public string sessionToken = null;
        private BetfairClient clientDelay = null;
        private BetfairClient clientActive = null;
        private string applicationKey = string.Empty;
        private int dateRange = 1;
        public List<HorseApiItem_Bf> arrangedHorseList;
        public Logger logger;
        public int debugMode = 0;

        public BetfairApiCtrl(BetfairAccount account)
        {
            m_cookieContainer = ReadCookiesFromDisk();
            m_account = account;
            applicationKey = ConfigurationManager.AppSettings["betfairApiKey"];

            arrangedHorseList = new List<HorseApiItem_Bf>();
            logger = new Logger();
            InitHttpClient();
        }

        private string getSessionId()
        {
            string sessionId = string.Empty;
            var loginPost = new FormUrlEncodedContent(new[]
            {
                              new KeyValuePair<string, string>("username",   m_account.UserName),
                              new KeyValuePair<string, string>("password", m_account.Password),
                      });
            m_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Application", applicationKey);
            m_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            HttpResponseMessage ipResponse = m_httpClient.PostAsync("https://identitysso.betfair.com.au/api/login", loginPost).Result;

            ipResponse.EnsureSuccessStatusCode();
            string strContent = ipResponse.Content.ReadAsStringAsync().Result;
            dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(strContent);
            if (jsonResponse["status"].ToString() == "SUCCESS")
            {
                sessionId = jsonResponse["token"].ToString();
            }
            return sessionId;
        }

        public bool doLogin(string username, string password)
        {
            HttpResponseMessage response = null;
            string strContent = "";
            try
            {
                bool bref = false; //getSessionToken();
                sessionToken = getSessionId();
                if (!string.IsNullOrEmpty(sessionToken))
                {
                    return true;
                }
                //if (bref)
                //    return true;

                response = m_httpClient.GetAsync("http://www.betfair.com/").Result;
                response.EnsureSuccessStatusCode();
                string mainReferer = response.RequestMessage.RequestUri.AbsoluteUri;
                if (string.IsNullOrEmpty(mainReferer))
                    return false;

                strContent = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(strContent))
                    return false;

                if (strContent.ToLower().Contains(m_account.UserName.ToLower()))
                {
                    bref = getSessionToken();
                    if (bref)
                        return true;
                }

                HtmlNode.ElementsFlags.Remove("form");
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(strContent);

                IEnumerable<HtmlNode> nodeForms = doc.DocumentNode.Descendants("form");
                if (nodeForms == null || nodeForms.LongCount() < 1)
                    return false;

                string action = nodeForms.ToArray()[0].GetAttributeValue("action", "");
                if (string.IsNullOrEmpty(action))
                    return false;

                IEnumerable<HtmlNode> nodeInputs =
                    nodeForms.ToArray()[0].Descendants("input").Where(node => node.Attributes["name"] != null);
                if (nodeInputs == null || nodeInputs.LongCount() < 1)
                    return false;

                string refererUrl = string.Empty;
                List<KeyValuePair<string, string>> inputs = new List<KeyValuePair<string, string>>();
                foreach (HtmlNode nodeInput in nodeInputs)
                {
                    string inputName = nodeInput.GetAttributeValue("name", "");
                    if (string.IsNullOrEmpty(inputName))
                        continue;

                    string inputValue = nodeInput.GetAttributeValue("value", "");
                    if (inputValue == null)
                        inputValue = string.Empty;

                    if (inputName == "username")
                        inputValue = username;

                    if (inputName == "password")
                        inputValue = password;

                    if (inputName == "url")
                        refererUrl = inputValue;

                    inputs.Add(new KeyValuePair<string, string>(inputName, inputValue));
                }

                m_httpClient.DefaultRequestHeaders.Referrer = new Uri(mainReferer);
                response = m_httpClient.PostAsync(action, (HttpContent)new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)inputs)).Result;
                response.EnsureSuccessStatusCode();

                strContent = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(strContent))
                    return false;

                doc.LoadHtml(strContent);

                nodeForms = doc.DocumentNode.Descendants("form").Where(node => node.Attributes["name"] != null && node.Attributes["name"].Value == "postLogin");
                if (nodeForms == null || nodeForms.LongCount() < 1)
                    return false;

                action = nodeForms.ToArray()[0].GetAttributeValue("action", "");
                if (string.IsNullOrEmpty(action))
                    return false;

                inputs = new List<KeyValuePair<string, string>>();
                foreach (HtmlNode nodeInput in nodeInputs)
                {
                    string name = nodeInput.GetAttributeValue("name", "");
                    if (string.IsNullOrEmpty(name))
                        continue;

                    string value = nodeInput.GetAttributeValue("value", "");
                    if (value == null)
                        value = string.Empty;

                    inputs.Add(new KeyValuePair<string, string>(name, value));
                }

                m_httpClient.DefaultRequestHeaders.Referrer = new Uri(string.Format("https://identitysso.betfair.com/view/login?product=prospect-page&url={0}",
                        WebUtility.UrlEncode(refererUrl)));

                response = m_httpClient.PostAsync(action, (HttpContent)new FormUrlEncodedContent((IEnumerable<KeyValuePair<string, string>>)inputs)).Result;
                response.EnsureSuccessStatusCode();

                strContent = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(strContent))
                    return false;

                if (!strContent.Contains(m_account.UserName))
                    return false;

                bool bflag = getSessionToken();
                if (!bflag)
                    return false;

                WriteCookiesToDisk(m_cookieContainer);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private bool getSessionToken()
        {

            CookieCollection cookies = m_cookieContainer.GetCookies(new Uri("https://www.betfair.com/"));
            if (cookies == null || cookies.Count < 1)
                return false;

            foreach (Cookie cookie in cookies)
            {
                if (cookie.Name == "ssoid")
                {
                    sessionToken = cookie.Value;
                    break;
                }
            }

            if (sessionToken != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void InitHttpClient()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 2;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = m_cookieContainer;
            m_httpClient = new HttpClient(handler);

            m_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            m_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.110 Safari/537.36");
            m_httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");

            m_httpClient.Timeout = new TimeSpan(0, 10, 0);
        }

        public void initApi(string username, string password, string key)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(key))
            {
                logger.severe("Invaild Login Parameters");
                return;
            }
            if (clientDelay == null)
            {
                if (string.IsNullOrEmpty(sessionToken))
                    doLogin(username, password);

                clientDelay = new BetfairClient(this.applicationKey, sessionToken);
            }
        }

        public List<HorseApiItem_Bf> getApiHorseList(int debugMode)
        {
            try
            {
                this.debugMode = debugMode;
                List<HorseApiItem_Bf> horseList = new List<HorseApiItem_Bf>();

                List<BetfairResItem> resList = doScrapHorse();

                if (resList.Count == 0 || resList == null)
                    return null;


                foreach (BetfairResItem item in resList)
                {
                    string raceNameStr = item.eventName;

                    foreach (BetfairMarketItem one in item.marketList)
                    {
                        string raceTime = one.raceTime;
                        string eachwayStr = one.eachwayStr;
                        string marketId_Win = one.marketId_Win;
                        string marketId_Place = one.marketId_Place;
                        int numberOfRunners = one.NumberOfRunners;
                        int NumberOfWinners = one.NumberOfWinners;

                        foreach (BetfairSelectionItem selectionItem in one.runnerList)
                        {
                            string runnerName = selectionItem.runnnerName;
                            if(runnerName.Equals("Capture The Drama"))
                            {
                                string eachwayStr1 = one.eachwayStr;
                            }
                            double winLayOdds = selectionItem.winLayOdds;
                            double winBackOdds = selectionItem.winBackOdds;

                            double placeLayOdds = selectionItem.placeLayOdds;
                            double placeBackOdds = selectionItem.placeBackOdds;
                            double eacyWayLayOdds = selectionItem.eachWayLayOdds;
                            double eacyWayBackOdds = selectionItem.eachWayBackOdds;
                            double winAvailableMoney = selectionItem.amountWinAvailableToLay;
                            double placeAvailableMoney = selectionItem.amountPlaceAvailableToLay;
                            double favPrice = selectionItem.favPrice;

                            HorseApiItem_Bf horseItem = new HorseApiItem_Bf();

                            horseItem.raceName = raceNameStr;
                            horseItem.raceTime = raceTime;
                            horseItem.runnerName = runnerName;
                            horseItem.raceName = raceNameStr;

                            horseItem.bfMarketId_Win = marketId_Win;
                            horseItem.bfMarketId_Place = marketId_Place;

                            horseItem.bfWinLay = winLayOdds.ToString();
                            horseItem.bfWinBack = winBackOdds.ToString();
                            horseItem.bfPlaceLay = placeLayOdds.ToString();
                            horseItem.bfPlaceBack = placeBackOdds.ToString();
                            horseItem.winAvailableMoney = winAvailableMoney.ToString();
                            horseItem.placeAvailableMoney = placeAvailableMoney.ToString();
                            horseItem.bfSelectionId = selectionItem.selectionId.ToString();
                            horseItem.numberOfRunners = numberOfRunners;
                            horseItem.numberOfWinners = NumberOfWinners;

                            if (!string.IsNullOrEmpty(eachwayStr))
                                horseItem.ew_divisor = "1/" + eachwayStr;

                            horseList.Add(horseItem);
                        }
                    }
                }

                return horseList;

            }
            catch (Exception ee)
            {
                if (debugMode == 1)
                {
                    logger.severe(ee.ToString());
                }
            }

            return null;
        }

        public void getFavPrice(ref List<BetfairResItem> raceList)
        {
            List<BetfairResItem> resList = new List<BetfairResItem>();

            try
            {
                foreach (BetfairResItem item in raceList)
                {
                    string raceNameStr = item.eventName;

                    foreach (BetfairMarketItem one in item.marketList)
                    {
                        double minOdds = 2000.0f;

                        List<BetfairSelectionItem> runnerList = new List<BetfairSelectionItem>();

                        foreach (BetfairSelectionItem selectionItem in one.runnerList)
                        {
                            double winBackOdds = selectionItem.winBackOdds;

                            if (minOdds >= winBackOdds) minOdds = winBackOdds;
                        }

                        foreach (BetfairSelectionItem selectionItem in one.runnerList)
                        {
                            selectionItem.favPrice = minOdds;
                        }

                    }
                }
            }
            catch (Exception ee)
            { 
            }
        }

        public List<BetfairResItem> doScrapHorse()
        {
            string sport = "Horse Racing";
            int eventType = 7;
            if (string.IsNullOrEmpty(sport) || eventType == 0)
                return null;

            List<BetfairResItem> bf_EventList = new List<BetfairResItem>();
            try
            {
                TimeRange startTimeRange = new TimeRange();
                startTimeRange.From = DateTime.Now;
                startTimeRange.To = DateTime.Today.AddDays(1);

                MarketFilter marketFilter = new MarketFilter();
                marketFilter.InPlayOnly = false;

                // Country
                ISet<string> countryName = new HashSet<string>();
                countryName.Add("GB");
                countryName.Add("IE");
                marketFilter.MarketCountries = countryName;

                //Time Range
                marketFilter.MarketStartTime = startTimeRange;

                //EventType Id
                ISet<string> eventTypeIds = new HashSet<string>();
                eventTypeIds.Add(eventType.ToString());
                marketFilter.EventTypeIds = eventTypeIds;


                ISet<string> marketTypes = new HashSet<string>();
                marketTypes.Add("WIN");
                marketTypes.Add("PLACE");
                marketTypes.Add("EACH_WAY");
                //marketTypes.Add("OTHER_PLACE");
                marketFilter.MarketTypeCodes = marketTypes;

                IList<EventResult> eventResultList = clientDelay.ListEvents(marketFilter).Result.Response;

                if (eventResultList == null || eventResultList.Count < 1)
                {
                    if(this.debugMode == 2) logger.debug("[BetFair][Soccer]Event Count : Null");
                    return null;
                }


                if (this.debugMode == 2)  logger.debug("[BetFair][Horse Racing]Event Count : " + eventResultList.Count.ToString());

                List<string> listRace = new List<string>();

                foreach (EventResult race in eventResultList)
                {
                    BetfairResItem bf_ResItem = new BetfairResItem();
                    bf_ResItem.eventId = race.Event.Id;
                    bf_ResItem.eventName = race.Event.Venue;

                    bf_ResItem.openTime = (DateTime)race.Event.OpenDate;

                    bf_ResItem.openTime = bf_ResItem.openTime.AddHours(2);
                    bf_ResItem.countryCode = race.Event.CountryCode;
                    bf_ResItem.marketList = new List<BetfairMarketItem>();

                    listRace.Add(race.Event.Id);
                    bf_EventList.Add(bf_ResItem);
                }

                ISet<MarketProjection> projection = new HashSet<MarketProjection>();

                projection.Add(MarketProjection.COMPETITION);
                projection.Add(MarketProjection.EVENT);
                projection.Add(MarketProjection.EVENT_TYPE);
                projection.Add(MarketProjection.RUNNER_DESCRIPTION);
                projection.Add(MarketProjection.RUNNER_METADATA);
                projection.Add(MarketProjection.MARKET_DESCRIPTION);
                projection.Add(MarketProjection.MARKET_START_TIME);

                List<string> listWinMarketId = new List<string>();
                List<string> listEachWayMarketId = new List<string>();
                List<string> listPlaceMarketId = new List<string>();

                List<BetfairMarketItem> total_MarketList = new List<BetfairMarketItem>();

                int nMarketCount = 1000;

                for (int m = 0; m < listRace.Count; m++)
                {
                    ISet<string> raceSet = new HashSet<string>();
                    raceSet.Add(listRace[m]);
                    marketFilter.EventIds = raceSet;

                    IList<MarketCatalogue> marketCatalogueList = clientDelay.ListMarketCatalogue(marketFilter, projection, null, nMarketCount).Result.Response;

                    string oldRaceTime = "";
                    int marketTypesCount = 0;
                    string eachWayDivStr = "";

                    foreach (MarketCatalogue catalogue in marketCatalogueList)
                    {
                        string catalogueTime = Utils.getTimeFormat(Convert.ToInt32(catalogue.Description.MarketTime.AddHours(1).TimeOfDay.TotalMinutes));

                        string marketTypeStr = catalogue.Description.MarketType;

                        if (marketTypeStr.Equals("WIN"))
                            listWinMarketId.Add(catalogue.MarketId);

                        if (marketTypeStr.Equals("PLACE"))
                            listPlaceMarketId.Add(catalogue.MarketId);

                        if (marketTypeStr.Equals("EACH_WAY"))
                            listEachWayMarketId.Add(catalogue.MarketId);

                        if (catalogueTime.Equals(oldRaceTime))
                        {
                            marketTypesCount++;
                        }

                        else
                        {
                            eachWayDivStr = "";
                            marketTypesCount = 0;
                        }

                        if (marketTypeStr.Equals("EACH_WAY"))
                        {
                            eachWayDivStr = catalogue.Description.EachWayDivisor.ToString();
                        }

                        if (marketTypesCount == 2)
                        {
                            BetfairMarketItem betfairMarketItem = new BetfairMarketItem();

                            betfairMarketItem.eventId = catalogue.Event.Id;

                            betfairMarketItem.marketId_Win = listWinMarketId.Last();
                            betfairMarketItem.marketId_EachWay = listEachWayMarketId.Last();
                            betfairMarketItem.marketId_Place = listPlaceMarketId.Last();

                            betfairMarketItem.raceTime = catalogueTime;

                            String marketNameStr = catalogue.MarketName;

                            if (!string.IsNullOrEmpty(eachWayDivStr))
                            {
                                betfairMarketItem.eachwayStr = eachWayDivStr;
                            }

                            betfairMarketItem.marketType = marketTypeStr;
                            betfairMarketItem.marketName = catalogue.MarketName;
                            betfairMarketItem.runnerList = new List<BetfairSelectionItem>();

                            foreach (var item in catalogue.Runners)
                            {
                                BetfairSelectionItem bf_SelectionItem = new BetfairSelectionItem();

                                bf_SelectionItem.selectionId = item.SelectionId.ToString();
                                bf_SelectionItem.runnnerName = item.RunnerName.ToString();

                                betfairMarketItem.runnerList.Add(bf_SelectionItem);
                            }

                            bf_EventList[m].marketList.Add(betfairMarketItem);
                        }

                        oldRaceTime = catalogueTime;
                    }
                }
               
                getWinPlaceOdd(listWinMarketId, ref bf_EventList, "WIN");
                getWinPlaceOdd(listEachWayMarketId, ref bf_EventList, "EACH-WAY");
                getWinPlaceOdd(listPlaceMarketId, ref bf_EventList, "PLACE");

                logger.debug("[GB&IE]Scrape End!");
            }
            catch (Exception ex)
            {
                 logger.severe("[Betfair][GB&IE]Error : " + ex.Message);
            }
            return bf_EventList;
        }

        private void getWinPlaceOdd(List<string> lismarketId_, ref List<BetfairResItem> bf_ResList, string marketType)
        {
            try
            {
                if (bf_ResList == null || bf_ResList.Count < 1)
                {
                    return;
                }

                PriceProjection priceProjection = new PriceProjection();
                ISet<PriceData> priceData = new HashSet<PriceData>();
                priceData.Add(PriceData.EX_BEST_OFFERS);
                priceData.Add(PriceData.EX_ALL_OFFERS);
                priceData.Add(PriceData.EX_TRADED);
                priceProjection.PriceData = priceData;
                int minLayAmount = 4;
                List<string> listIdList = new List<string>();

                listIdList = lismarketId_;

                ISet<string> lisIds = new HashSet<string>();
                int n = 0;
                for (int m = 0; m < listIdList.Count; m++)
                {
                    n++;
                    lisIds.Add(listIdList[m]);
                    if (n % 6 == 0)
                    {
                        IList<MarketBook> marketBookList = clientDelay.ListMarketBook(lisIds, priceProjection, null, null).Result.Response;
                        if (marketBookList == null || marketBookList.Count < 1)
                        {
                            continue;
                        }

                        foreach (MarketBook marketBook in marketBookList)
                        {
                            foreach (BetfairResItem betfairEventItem in bf_ResList)
                            {
                                foreach (BetfairMarketItem bf_MarketItem in betfairEventItem.marketList)
                                {
                                    if (bf_MarketItem.marketId_Win == marketBook.MarketId && marketType.Equals("WIN"))
                                    {
                                        try
                                        {
                                            if (marketBook.Runners == null || marketBook.Runners.Count < 1)
                                                break;

                                            bf_MarketItem.marketId_Win = marketBook.MarketId;

                                            foreach (Runner runner in marketBook.Runners)
                                            {
                                                for (int i = 0; i < bf_MarketItem.runnerList.Count; i++)
                                                {
                                                    if (runner.SelectionId.ToString() == bf_MarketItem.runnerList[i].selectionId)
                                                    {
                                                        try
                                                        {
                                                            double value = runner.ExchangePrices.AvailableToLay[0].Price;
                                                            double backValue = runner.ExchangePrices.AvailableToBack[0].Price;

                                                            double amountWinAvailableToLay = runner.ExchangePrices.AvailableToLay[0].Size;
                                                            double amountWinAvailableToBack = runner.ExchangePrices.AvailableToBack[0].Size;
                                                            if (amountWinAvailableToLay < minLayAmount)
                                                            {
                                                                bf_MarketItem.runnerList[i].winLayOdds = 200;
                                                            }
                                                            else
                                                            {
                                                                bf_MarketItem.runnerList[i].winLayOdds = value;
                                                            }
                                                            if(bf_MarketItem.runnerList[i].runnnerName.Equals("Capture The Drama") )
                                                            {
                                                                if (this.debugMode == 2) logger.debug(bf_MarketItem.runnerList[i].runnnerName + "  " + amountWinAvailableToLay);
                                                            }
                                                            if (this.debugMode == 2)  logger.debug(bf_MarketItem.runnerList[i].runnnerName + "  " + amountWinAvailableToLay);

                                                            bf_MarketItem.runnerList[i].winBackOdds = backValue;
                                                            bf_MarketItem.runnerList[i].amountWinAvailableToLay = amountWinAvailableToLay;
                                                        }
                                                        catch (Exception ex) { }
                                                        break;

                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.debug("[Betfair]Error(getValue) :" + ex.Message);
                                        }
                                        break;
                                    }

                                    if (bf_MarketItem.marketId_EachWay == marketBook.MarketId && marketType.Equals("EACH-WAY"))
                                    {
                                        try
                                        {
                                            if (marketBook.Runners == null || marketBook.Runners.Count < 1)
                                                break;

                                            foreach (Runner runner in marketBook.Runners)
                                            {
                                                for (int i = 0; i < bf_MarketItem.runnerList.Count; i++)
                                                {
                                                    if (runner.SelectionId.ToString() == bf_MarketItem.runnerList[i].selectionId)
                                                    {
                                                        try
                                                        {
                                                            double value = runner.ExchangePrices.AvailableToLay[0].Price;
                                                            double backValue = runner.ExchangePrices.AvailableToBack[0].Price;

                                                            double amountWinAvailableToLay = runner.ExchangePrices.AvailableToLay[0].Size;
                                                            if (amountWinAvailableToLay < minLayAmount)
                                                            {
                                                                bf_MarketItem.runnerList[i].eachWayLayOdds = 200;
                                                            }
                                                            else
                                                            {
                                                                bf_MarketItem.runnerList[i].eachWayLayOdds = value;
                                                            }
                                                            if (bf_MarketItem.runnerList[i].runnnerName.Equals("Capture The Drama"))
                                                            {
                                                                if (this.debugMode == 2) logger.debug(bf_MarketItem.runnerList[i].runnnerName + "  " + amountWinAvailableToLay);
                                                            }
                                                            if (this.debugMode == 2) logger.debug(bf_MarketItem.runnerList[i].runnnerName + "  " + amountWinAvailableToLay);

                                                            bf_MarketItem.runnerList[i].eachWayBackOdds = backValue;
                                                        }
                                                        catch (Exception ex) { }
                                                        break;

                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.severe("[Betfair]Error(getValue) :" + ex.Message);
                                        }
                                        break;

                                    }

                                    if (bf_MarketItem.marketId_Place == marketBook.MarketId && marketType.Equals("PLACE"))
                                    {
                                        try
                                        {
                                            if (marketBook.Runners == null || marketBook.Runners.Count < 1)
                                                break;

                                            bf_MarketItem.marketId_Place = marketBook.MarketId;
                                            bf_MarketItem.NumberOfRunners = marketBook.NumberOfActiveRunners;
                                            bf_MarketItem.NumberOfWinners = marketBook.NumberOfWinners;

                                            if (!checkRunners(bf_MarketItem.NumberOfRunners, bf_MarketItem.NumberOfWinners))
                                                continue;

                                            foreach (Runner runner in marketBook.Runners)
                                            {
                                                for (int i = 0; i < bf_MarketItem.runnerList.Count; i++)
                                                {
                                                    if (runner.SelectionId.ToString() == bf_MarketItem.runnerList[i].selectionId)
                                                    {
                                                        try
                                                        {
                                                            double value = runner.ExchangePrices.AvailableToLay[0].Price;
                                                            double backValue = runner.ExchangePrices.AvailableToBack[0].Price;

                                                            double amountPlaceAvailableToLay = runner.ExchangePrices.AvailableToLay[0].Size;
                                                            if (amountPlaceAvailableToLay <= minLayAmount)
                                                            {
                                                                bf_MarketItem.runnerList[i].placeLayOdds = 200;
                                                            }
                                                            else
                                                            {
                                                                bf_MarketItem.runnerList[i].placeLayOdds = value;
                                                            }
                                                            if (this.debugMode == 2)  logger.debug(bf_MarketItem.runnerList[i].runnnerName + "  " + amountPlaceAvailableToLay);
                                                            bf_MarketItem.runnerList[i].placeBackOdds = backValue;
                                                            bf_MarketItem.runnerList[i].amountPlaceAvailableToLay = amountPlaceAvailableToLay;
                                                        }
                                                        catch (Exception ex) { }
                                                        break;

                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.severe("[Betfair]Error(getValue) :" + ex.Message);
                                        }
                                        break;
                                    }
                                }
                            }
                        }

                        lisIds = new HashSet<string>();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.severe("[Betfair]Error(getWinPlaceOdd) : " + ex.Message);
            }

            return;
        }

        public string getMarketId(List<string> marketIdList, string marketId)
        {
            try
            {
                foreach (string item in marketIdList)
                {
                    if (item.Equals(marketId))
                        return marketId;
                }

                return "";
            }
            catch (Exception ee)
            {
            }

            return "";
        }

        private void getValue(MarketBook marketBook, ref BetfairMarketItem raceModel)
        {
            try
            {
                if (marketBook.Runners == null || marketBook.Runners.Count < 1)
                    return;

                foreach (Runner runner in marketBook.Runners)
                {
                    for (int i = 0; i < raceModel.runnerList.Count; i++)
                    {
                        if (runner.SelectionId.ToString() == raceModel.runnerList[i].selectionId)
                        {
                            if (runner.SelectionId.ToString() == "1408743")
                            {
                                string d = "";
                            }
                            try
                            {
                                double value = runner.ExchangePrices.AvailableToLay[0].Price;
                                raceModel.runnerList[i].winLayOdds = value;
                            }
                            catch (Exception ex) { }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.severe("[Betfair]Error(getValue) :" + ex.Message);

            }

            return;
        }


        public bool checkRunners(int selectionCount, int placeCount)
        {
            try
            {
                if (selectionCount >= 16 && placeCount == 4)
                    return true;

                if (selectionCount >= 8 && selectionCount < 16)
                {
                    if (placeCount == 3)
                        return true;
                    else
                        return false;
                }

                if (selectionCount >= 4 && selectionCount < 8)
                {
                    if (placeCount == 2)
                        return true;
                    else
                        return false;
                }
            }
            catch (Exception ee)
            {
            }

            return false;
        }

        public void getEvents(string sport, int eventType)
        {
            if (string.IsNullOrEmpty(sport) || eventType == 0)
                return;
            try
            {
                MarketFilter marketFilter = new MarketFilter();
                marketFilter.InPlayOnly = false;
                ISet<string> eventTypeIds = new HashSet<string>();
                marketFilter.EventTypeIds = eventTypeIds;
                eventTypeIds.Add(eventType.ToString());

                IList<EventResult> eventResultList = clientDelay.ListEvents(marketFilter).Result.Response;

                if (eventResultList == null || eventResultList.Count < 1)
                    return;

                ISet<string> listRace = new HashSet<string>();
                foreach (EventResult race in eventResultList)
                {
                    if (race.Event.CountryCode != null)
                    {
                        if (race.Event.CountryCode.Contains("GB") && race.Event.Venue != null)
                        {

                            listRace.Add(race.Event.Id);
                        }
                    }
                }

                var raceList = clientDelay.ListRaceDetails(listRace, null).Result;

                List<string> raceids = new List<string>();
                foreach (var race in raceList.Response)
                {
                    raceids.Add(race.RaceId);
                }

                raceids.Add("1.174788821");

                IList<MarketBook> marketList = clientDelay.ListMarketBook(raceids).Result.Response;
            }
            catch (Exception ex)
            {

            }
        }

        #region Cookie Control
        public void WriteCookiesToDisk(CookieContainer cookieJar)
        {
            try
            {
                string m_path = System.Web.Hosting.HostingEnvironment.MapPath("/");

                string cookieFilePath = string.Format("BetfairAcc-cookie.bin");

                using (Stream stream = File.Create(m_path + "\\" + cookieFilePath))
                {
                    try
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, cookieJar);
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
            catch (Exception e)
            {
                logger.severe(e.Message);
            }
        }

        public CookieContainer ReadCookiesFromDisk()
        {
            CookieContainer cookieJar = new CookieContainer();
            try
            {
                string m_path = System.Web.Hosting.HostingEnvironment.MapPath("/");

                string cookieFilePath = string.Format("BetfairAcc-cookie.bin");
                using (Stream stream = File.Open(m_path + "\\" + cookieFilePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    cookieJar = (CookieContainer)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                cookieJar = new CookieContainer();
            }
            return cookieJar;
        }

        public void writeStatus(string status)
        {
            try
            {
                string logFilepath = System.Web.Hosting.HostingEnvironment.MapPath("/");

                string logText = (string.Format("[{0}] {1}", Utils.getCurrentUKTime().ToString("yyyy-MM-dd HH:mm:ss"), status));
                LogToFile(logFilepath + "\\Log\\" + string.Format("log_{0}.txt", Utils.getCurrentUKTime().ToString("yyyy-MM-dd")), logText);
            }
            catch (Exception)
            {

            }
        }

        private void LogToFile(string filename, string result)
        {
            try
            {
                if (string.IsNullOrEmpty(filename))
                    return;
                StreamWriter streamWriter = new StreamWriter((Stream)System.IO.File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8);
                if (!string.IsNullOrEmpty(result))
                    streamWriter.WriteLine(result);
                streamWriter.Close();
            }
            catch (System.Exception ex)
            {
            }
        }

        #endregion
    }
}
