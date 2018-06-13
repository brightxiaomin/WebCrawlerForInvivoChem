using Abot.Crawler;
using Abot.Poco;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;

namespace Abot.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            PrintDisclaimer();

            Uri uriToCrawl = new Uri("https://www.invivochem.com");

            //http://www.invivochem.com/venetoclax-abt-199-or-gdc-0199/

            //http://www.invivochem.com/abt-737/
            //http://www.invivochem.com/abt-737/
            //https://www.invivochem.com/melk-8a/

            IWebCrawler crawler;

            //Uncomment only one of the following to see that instance in action
            crawler = GetCustomBehaviorUsingLambdaWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            //crawler = GetCustomBehaviorUsingLambdaWebCrawler();

            //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
            //This is where you process data about specific events of the crawl
            crawler.PageCrawlStartingAsync += Crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += Crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += Crawler_PageLinksCrawlDisallowed;

            //Start the crawl
            //This is a synchronous call
            CrawlResult result = crawler.Crawl(uriToCrawl);

            //Now go view the log.txt file that is in the same directory as this executable. It has
            //all the statements that you were trying to read in the console window :).
            //Not enough data being logged? Change the app.config file's log4net log level from "INFO" TO "DEBUG"

            PrintDisclaimer();
        }

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new PoliteWebCrawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
            //Create a config object manually
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = false;
            config.IsExternalPageLinksCrawlingEnabled = false;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 10;
            config.MaxPagesToCrawl = 10;
            config.MaxPagesToCrawlPerDomain = 0;
            config.MinCrawlDelayPerDomainMilliSeconds = 1000;

            //Add you own values without modifying Abot's source code.
            //These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
            config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, null, null, null, null);
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetDefaultWebCrawler();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                string uri = pageToCrawl.Uri.AbsoluteUri;
                if (uri.Contains("wp-content") || uri.Contains("abt-333") || uri.Contains("pyrrolidinedithi%E") || uri.Contains("ots514hcl"))
                    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                if(uri.Contains("invivochem.com"))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            //crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            //{
            //    if (crawlContext.CrawledCount >= 5)
            //        return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

            //    return new CrawlDecision { Allow = true };
            //});

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            //crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            //{
            //    if (!crawledPage.IsInternal)
            //        return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

            //    return new CrawlDecision { Allow = true };
            //});

            return crawler;
        }

        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInput = "";
            if (args.Length < 1)
            {
                System.Console.WriteLine("Please enter ABSOLUTE url to crawl:");
                userInput = System.Console.ReadLine();
            }
            else
            {
                userInput = args[0];
            }

            if (string.IsNullOrWhiteSpace(userInput))
                throw new ApplicationException("Site url to crawl is as a required parameter");

            return new Uri(userInput);
        }

        private static void PrintDisclaimer()
        {
            PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }

        static void Crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data
        }

        static void Crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            string content = crawledPage.Content.Text;

            //string filePathHtml = @"C:\Users\mxiao\Documents\debug.html";
            //File.WriteAllText(filePathHtml, content);

            string patternForCatalogNumber = @"(?<=Cat #:\s?)([A-Za-z0-9]{1,5})"; 
            Regex rgxForCatalogNumber = new Regex(patternForCatalogNumber, RegexOptions.IgnoreCase);
            Match matchCatalog = rgxForCatalogNumber.Match(content); 

            if(matchCatalog.Success)
            {
                string patternForCasNumber = @"(?<=Cas\s?#:\s?)([0-9-]{5,12})";
                Regex rgxForCasNumber = new Regex(patternForCasNumber, RegexOptions.IgnoreCase);
                Match matchCas = rgxForCasNumber.Match(content);

                string patternForProductName = @"(?<=Description:\s*)([\w-]+)";
                Regex rgxForProductName = new Regex(patternForProductName, RegexOptions.IgnoreCase);
                Match matchProductName = rgxForProductName.Match(content);

                string patternForQuantity = "(?<=\"ptp-plan\">)(\\d{1,3}m?g)";
                Regex rgxForQuantity = new Regex(patternForQuantity, RegexOptions.IgnoreCase);
                MatchCollection matchesForQuantity = rgxForQuantity.Matches(content);

                string patternForPrice = "(?<=\"ptp-price\">)(\\$|&#36;)[\\d,]{2,6}"; 
                Regex rgxForPrice = new Regex(patternForPrice, RegexOptions.IgnoreCase);
                MatchCollection matchesForPrice = rgxForPrice.Matches(content);

                if(matchCas.Success && matchProductName.Success)
                {
                    string filePathCSV = @"C:\Users\mxiao\Documents\Product.csv";
                    string filePathTXT = @"C:\Users\mxiao\Documents\Product.txt";
                    string filePathSQL = @"C:\Users\mxiao\Documents\invivochem.sql";

                    string entryCSV = matchCatalog.Value + "," + matchCas.Value + "," + matchProductName.Value;
                    string sqlEntry = "INSERT INTO dbo.Product (CatalogNumber, CASNumber, Name) VALUES('" + matchCatalog.Value + "', '" + matchCas.Value + "', '" + matchProductName.Value + "')" + "\n";

                    int count = Math.Min(matchesForPrice.Count, matchesForQuantity.Count);

                    string priceForCSV = "";
                    string priceForSQL = "";
                    string amountForCSV = "";
                    string amountForSQL = "";

                    for (int i = 0; i < count; i++)
                    {
                        priceForCSV = matchesForPrice[i].Value.Replace(",", "").Replace("&#36;", "$");
                        priceForSQL = priceForCSV.Replace("$", "");
                        amountForCSV = matchesForQuantity[i].Value;
                        amountForSQL = amountForCSV.Replace("mg", "").Replace("g", "000");
                        entryCSV += "," + priceForCSV + "/" + amountForCSV;
                        sqlEntry += "INSERT INTO dbo.Price (CASNumber, Price, Amount) VALUES('" + matchCas.Value + "'," + priceForSQL + "," + amountForSQL + ")" + "\n";
                    }

                    entryCSV += "\n";
                    sqlEntry += "\n";

                    File.AppendAllText(filePathCSV, entryCSV);
                    File.AppendAllText(filePathTXT, entryCSV);
                    File.AppendAllText(filePathSQL, sqlEntry);
                }
            }
        }

        static void Crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void Crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }
    }
}