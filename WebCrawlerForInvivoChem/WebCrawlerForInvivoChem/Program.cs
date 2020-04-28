using Abot.Crawler;
using Abot.Poco;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections;
using System.Threading;

namespace Abot.Demo
{
    class Program
    {
        static ReaderWriterLock rwl = new ReaderWriterLock();
        //static int writes = 0;
        static int writerTimeouts = 0;

        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            PrintDisclaimer();

            #region InvivoChem
            Uri uriToCrawl = new Uri("https://www.invivochem.com/all-products/");
            //http://www.invivochem.com/venetoclax-abt-199-or-gdc-0199/
            //http://www.invivochem.com/abt-737/
            //http://www.invivochem.com/a-1210477/
            IWebCrawler crawler;
            //Uncomment only one of the following to see that instance in action
            //crawler = GetDefaultWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            crawler = GetCustomBehaviorUsingLambdaWebCrawler();

            //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
            //This is where you process data about specific events of the crawl
            //crawler.PageCrawlStartingAsync += Crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompleted;
            //crawler.PageCrawlDisallowedAsync += Crawler_PageCrawlDisallowed;
            //crawler.PageLinksCrawlDisallowedAsync += Crawler_PageLinksCrawlDisallowed;
            #endregion


            #region SelleckChem

            //Uri uriToCrawl = new Uri("http://www.selleckchem.com");
            ////http://www.selleckchem.com/products/bicuculline.html
            ////http://www.selleckchem.com

            //IWebCrawler crawler;
            //crawler = GetDefaultWebCrawler();
            //crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompletedSelleckchem;
            #endregion

            #region MedKoo
            ////ABOUT 50 * 342 PRODUCTS, ABOUT 15000
            //Uri uriToCrawl = new Uri("https://www.medkoo.com/products");
            ////https://www.medkoo.com/products/33278
            ////https://www.medkoo.com/products
            ////26745

            //IWebCrawler crawler;
            //crawler = GetCustomWebCrawlerForMedkoo();
            //crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompletedMedkoo;
            #endregion


            #region MCE
            //Uri uriToCrawl = new Uri("https://www.medchemexpress.com/ABT-199.html");
            ////https://www.medchemexpress.com


            //IWebCrawler crawler;
            //crawler = GetDefaultWebCrawler();
            //crawler.PageCrawlCompletedAsync += Crawler_ProcessPageCrawlCompletedMCE;

            #endregion

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

                if (uri.Contains("invivochem.com"))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false, Reason = "External" };
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

        private static IWebCrawler GetCustomWebCrawlerForSelleckchem()
        {
            //try disable External First, then try enable exteral.
            IWebCrawler crawler = GetDefaultWebCrawler();

            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                string uri = pageToCrawl.Uri.AbsoluteUri;

                if (uri.Contains("selleckchem.com"))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false, Reason = "External" };
            });

            return crawler;
        }

        private static IWebCrawler GetCustomWebCrawlerForMedkoo()
        {
            //try disable External First, then try enable exteral.
            IWebCrawler crawler = GetDefaultWebCrawler();

            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                string uri = pageToCrawl.Uri.AbsoluteUri;

                if (uri.Contains("medkoo.com"))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false, Reason = "External" };
            });

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

        static void Crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void Crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void Crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            string content = crawledPage.Content.Text;

            //string filePathHtml = @"C:\Users\mxiao\Documents\Debug.html";
            //File.WriteAllText(filePathHtml, content);

            string patternForCatalogNumber = @"((?<=Cat\s*#:\s*)|(?<=Catalog\s*#:\s*))([A-Za-z0-9]{1,5})";
            Regex rgxForCatalogNumber = new Regex(patternForCatalogNumber, RegexOptions.IgnoreCase);
            Match matchCatalog = rgxForCatalogNumber.Match(content);

            if (matchCatalog.Success)
            {
                string patternForCasNumber = @"(?<=Cas\s*#:\s*)([0-9-]{5,12})";
                Regex rgxForCasNumber = new Regex(patternForCasNumber, RegexOptions.IgnoreCase);
                Match matchCas = rgxForCasNumber.Match(content);

                string patternForProductName = @"((?<=Description:\s*)|(?<=Description</strong>:\s*)|(?<=Description:</strong>\s*))([\w()+-]+)";
                Regex rgxForProductName = new Regex(patternForProductName, RegexOptions.IgnoreCase);
                Match matchProductName = rgxForProductName.Match(content);

                //Add Description content
                string patternForDescription = @"((?<=Description:\s*)|(?<=Description</strong>:\s*)|(?<=Description:</strong>\s*))(.+\.\s)";
                Regex rgxForDescription = new Regex(patternForDescription, RegexOptions.IgnoreCase);
                Match matchDescription = rgxForDescription.Match(content);

                //place holder for formula
                //string test = @"C<sub>45</sub>H<sub>50</sub>ClN<sub>7</sub>O<sub>7</sub>S</span>";
                //test = test.Replace("<sub>", "").Replace(@"</sub>", "").Replace(@"</span>", "");

                //Add Formula
                string patternForFormula = "((?<=Formula</span>[\\s\\S]*13px;\">)|(?<=Formula</span>[\\s\\S]*Helvetica;\">)).*</span>";
                Regex rgxForFormula = new Regex(patternForFormula, RegexOptions.IgnoreCase);
                Match matchFormula = rgxForFormula.Match(content);

                string patternForQuantity = "(?<=\"ptp-plan\">)(\\d{1,3}m?g)";
                Regex rgxForQuantity = new Regex(patternForQuantity, RegexOptions.IgnoreCase);
                MatchCollection matchesForQuantity = rgxForQuantity.Matches(content);

                string patternForPrice = "(?<=\"ptp-price\">)(\\$|&#36;)[\\d,]{2,6}";
                Regex rgxForPrice = new Regex(patternForPrice, RegexOptions.IgnoreCase);
                MatchCollection matchesForPrice = rgxForPrice.Matches(content);

                if (matchCas.Success)
                {
                    string filePathCSV = @"C:\Users\mxiao\Documents\Product.csv";
                    //string filePathTXT = @"C:\Users\mxiao\Documents\Product.txt"; 
                    //string filePathSQL = @"C:\Users\mxiao\Documents\invivochem.sql";
                    string name = matchProductName.Success ? matchProductName.Value : string.Empty;
                    string desc = matchDescription.Success ? matchDescription.Value : string.Empty;
                    desc = desc.Replace(",", "|");
                    string formula = matchFormula.Success ? matchFormula.Value : string.Empty;
                    formula = formula.Replace("<sub>", "").Replace(@"</sub>", "").Replace(@"</span>", "");

                    string entryCSV = matchCatalog.Value + "," + matchCas.Value + "," + formula + "," + name + "," + desc;
                    //string sqlEntry = "INSERT INTO dbo.Product (CatalogNumber, CASNumber, Name) VALUES('" + matchCatalog.Value + "', '" + matchCas.Value + "', '" + name + "')" + "\n";

                    int count = Math.Min(matchesForPrice.Count, matchesForQuantity.Count);

                    string priceForCSV = "";
                    //string priceForSQL = "";
                    string amountForCSV = "";
                    //string amountForSQL = "";

                    for (int i = 0; i < count; i++)
                    {
                        priceForCSV = matchesForPrice[i].Value.Replace(",", "").Replace("&#36;", "$");
                        //priceForSQL = priceForCSV.Replace("$", "");
                        amountForCSV = matchesForQuantity[i].Value;
                        //amountForSQL = amountForCSV.Replace("mg", "").Replace("g", "000");
                        entryCSV += "," + priceForCSV + "/" + amountForCSV;
                        //sqlEntry += "INSERT INTO dbo.Price (CASNumber, Price, Amount) VALUES('" + matchCas.Value + "'," + priceForSQL + "," + amountForSQL + ")" + "\n";
                    }

                    entryCSV += "\n";
                    //sqlEntry += "\n";
                    try
                    {
                        rwl.AcquireWriterLock(100);
                        try
                        {
                            //Write To File
                            File.AppendAllText(filePathCSV, entryCSV);
                            //File.AppendAllText(filePathTXT, entryCSV);
                            //File.AppendAllText(filePathSQL, sqlEntry);
                        }
                        finally
                        {
                            // Ensure that the lock is released.
                            rwl.ReleaseWriterLock();
                        }
                    }
                    catch (ApplicationException)
                    {
                        // The writer lock request timed out.
                        Interlocked.Increment(ref writerTimeouts);
                    }

                }
            }
        }

        static void Crawler_ProcessPageCrawlCompletedSelleckchem(object sender, PageCrawlCompletedArgs e) 
        {
            CrawledPage crawledPage = e.CrawledPage;
            string content = crawledPage.Content.Text;
            bool isProduct = crawledPage.Uri.AbsoluteUri.Contains("/products/");

            if(isProduct)
            {
                //string filePathHtml = @"C:\Users\mxiao\Documents\selleckchem.html";
                //File.WriteAllText(filePathHtml, content);

                string patternForCatalogNumber = "(?<=Catalog No.)([A-Za-z0-9]{1,6})";
                Regex rgxForCatalogNumber = new Regex(patternForCatalogNumber, RegexOptions.IgnoreCase);
                Match matchCatalog = rgxForCatalogNumber.Match(content);

                if (matchCatalog.Success)
                {
                    string patternForCasNumber = "(?<=CAS No.</th>\\s*\n?\\s*<td>\\s*)([0-9-]{5,12})";
                    Regex rgxForCasNumber = new Regex(patternForCasNumber, RegexOptions.IgnoreCase);
                    Match matchCas = rgxForCasNumber.Match(content);

                    string patternForProductName = "(?<=<h1 class=\"fl\">\\s*)([\\w()+-]+)";
                    Regex rgxForProductName = new Regex(patternForProductName, RegexOptions.IgnoreCase);
                    Match matchProductName = rgxForProductName.Match(content);

                    string patternForQuantity = "(?<=width=\"133px\">\\s*\n?\\s*<label>\\s*)(\\d{1,3}m?(g|M))";
                    Regex rgxForQuantity = new Regex(patternForQuantity, RegexOptions.IgnoreCase);
                    MatchCollection matchesForQuantity = rgxForQuantity.Matches(content);

                    string patternForPrice = "(?<=<td width=\"86px\">\\s*\n?\\s*USD\\s*)[\\d,]{1,9}";
                    Regex rgxForPrice = new Regex(patternForPrice, RegexOptions.IgnoreCase);
                    MatchCollection matchesForPrice = rgxForPrice.Matches(content);

                    if (matchCas.Success)
                    {
                        string filePathCSV = @"C:\Users\mxiao\Documents\SelleckchemProduct.csv";
                        string name = matchProductName.Success ? matchProductName.Value : string.Empty;

                        string entryCSV = matchCatalog.Value + "," + matchCas.Value + "," + name;

                        int count = Math.Min(matchesForPrice.Count, matchesForQuantity.Count);

                        for (int i = 0; i < count; i++)
                        {
                            entryCSV += "," + matchesForPrice[i].Value.Replace(",", "") + "/" + matchesForQuantity[i].Value;
                        }

                        entryCSV += "\n";
                        try
                        {
                            rwl.AcquireWriterLock(100);
                            try
                            {
                                //Write To File
                                File.AppendAllText(filePathCSV, entryCSV);
                            }
                            finally
                            {
                                // Ensure that the lock is released.
                                rwl.ReleaseWriterLock();
                            }
                        }
                        catch (ApplicationException)
                        {
                            // The writer lock request timed out.
                            //throw;
                        }

                    }
                }
            }            
        }

        static void Crawler_ProcessPageCrawlCompletedMedkoo(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            string content = crawledPage.Content.Text;

            //string filePathHtml = @"C:\Users\mxiao\Documents\Medkoo.html";
            //File.WriteAllText(filePathHtml, content);

            string patternForCatalogNumber = @"(?<=MedKoo CAT#:\s*)([A-Za-z0-9]{1,6})";
            Regex rgxForCatalogNumber = new Regex(patternForCatalogNumber, RegexOptions.IgnoreCase);
            Match matchCatalog = rgxForCatalogNumber.Match(content);

            if (matchCatalog.Success)
            {
                string patternForCasNumber = @"(?<=CAS#:\s*)([0-9-]{5,12})";
                Regex rgxForCasNumber = new Regex(patternForCasNumber, RegexOptions.IgnoreCase);
                Match matchCas = rgxForCasNumber.Match(content);

                string patternForProductName = @"(?<=Name:\s*)([\w()+-]+)";
                Regex rgxForProductName = new Regex(patternForProductName, RegexOptions.IgnoreCase);
                Match matchProductName = rgxForProductName.Match(content);


                string patternForFormula = @"(?<=Chemical Formula:\s*)(\w+)";
                Regex rgxForFormula = new Regex(patternForFormula, RegexOptions.IgnoreCase);
                Match matchFormula = rgxForFormula.Match(content);

                string patternForMW = @"(?<=Molecular Weight:\s*)([\d.]+)";
                Regex rgxForMW = new Regex(patternForMW, RegexOptions.IgnoreCase);
                Match matchMW = rgxForMW.Match(content);


                string patternForChemicalName = @"(?<=IUPAC/Chemical Name:\s*\n?\s*</strong>\s*\n?\s*)([\w()\+\-\[\],\'.\:]+)";
                Regex rgxForChemicalName = new Regex(patternForChemicalName, RegexOptions.IgnoreCase);
                Match matchChemicalName = rgxForChemicalName.Match(content);

                string patternForQuantity = "(?<=class='col-xs-4'>\\s*\n?\\s*)(\\d{1,3}m?g)";
                Regex rgxForQuantity = new Regex(patternForQuantity, RegexOptions.IgnoreCase);
                MatchCollection matchesForQuantity = rgxForQuantity.Matches(content);

                string patternForPrice = "(?<=class='col-xs-8'>\\s*\n?USD\\s*)[\\d,]{1,9}";
                Regex rgxForPrice = new Regex(patternForPrice, RegexOptions.IgnoreCase);
                MatchCollection matchesForPrice = rgxForPrice.Matches(content);

                // for description
                string patternForDescription = @"(?<=Description:\s*\n?\s*</strong>\s*\n?\s*)([\w()\+\-\[\],\'\:\s]+)";
                Regex rgxForDescription = new Regex(patternForDescription, RegexOptions.IgnoreCase);
                Match matchDescription = rgxForDescription.Match(content);

                if (matchCas.Success)
                {
                    string filePathCSV = @"C:\Users\mxiao\Documents\MedkooProduct.csv";
                    string name = matchProductName.Success ? matchProductName.Value : string.Empty;

                    string formula = matchFormula.Success ? matchFormula.Value : string.Empty;
                    string mw = matchMW.Success ? matchMW.Value : string.Empty;
                    // here I am replacing , with _|, replace it back in the final excel file.
                    string chemicalName = matchChemicalName.Success ? matchChemicalName.Value : string.Empty;
                    chemicalName = chemicalName.Replace(",", "_|");

                    string description = matchDescription.Success ? matchDescription.Value : string.Empty;
                    description = description.Replace(",", "_|").Trim();


                    string entryCSV = matchCatalog.Value + "," + matchCas.Value + "," + name + "," + formula + "," + mw + "," + chemicalName + "," + description;

                    int count = Math.Min(matchesForPrice.Count, matchesForQuantity.Count);

                    for (int i = 0; i < count; i++)
                    {
                        entryCSV += "," + matchesForPrice[i].Value.Replace(",", "") + "/" + matchesForQuantity[i].Value;
                    }

                    entryCSV += "\n";
                    try
                    {
                        rwl.AcquireWriterLock(200);
                        try
                        {
                            //Write To File
                            File.AppendAllText(filePathCSV, entryCSV);
                        }
                        finally
                        {
                            // Ensure that the lock is released.
                            rwl.ReleaseWriterLock();
                        }
                    }
                    catch (ApplicationException)
                    {
                        // The writer lock request timed out.
                        throw;
                    }

                }
            }
        }

        static void Crawler_ProcessPageCrawlCompletedMCE(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            string content = crawledPage.Content.Text;

            string filePathHtml = @"C:\Users\mxiao\Documents\MCE.html";
            File.WriteAllText(filePathHtml, content);

            string patternForCatalogNumber = @"(?<=Cat. No.:\s*)([A-Za-z0-9-]{1,10})";
            Regex rgxForCatalogNumber = new Regex(patternForCatalogNumber, RegexOptions.IgnoreCase);
            Match matchCatalog = rgxForCatalogNumber.Match(content);

            if (matchCatalog.Success)
            {
                string patternForCasNumber = @"(?<=CAS No.\s*:\s*<span>)([0-9-]{5,12})";
                Regex rgxForCasNumber = new Regex(patternForCasNumber, RegexOptions.IgnoreCase);
                Match matchCas = rgxForCasNumber.Match(content);

                string patternForProductName = "(?<=<h1 itemprop=\"name\"><strong>)([\\w\\(\\)\\+\\-]+)";
                Regex rgxForProductName = new Regex(patternForProductName, RegexOptions.IgnoreCase);
                Match matchProductName = rgxForProductName.Match(content);

                string patternForQuantity = "(?<=width=\"133px\">\\s*\n?\\s*<label>)(\\d{1,3}m?(g|M))";
                Regex rgxForQuantity = new Regex(patternForQuantity, RegexOptions.IgnoreCase);
                MatchCollection matchesForQuantity = rgxForQuantity.Matches(content);

                string patternForPrice = "(?<=<td width=\"86px\">\\s*\n?\\s*USD\\s*)[\\d,]{1,9}";
                Regex rgxForPrice = new Regex(patternForPrice, RegexOptions.IgnoreCase);
                MatchCollection matchesForPrice = rgxForPrice.Matches(content);

                if (matchCas.Success)
                {
                    string filePathCSV = @"C:\Users\mxiao\Documents\MCEProduct.csv";
                    string name = matchProductName.Success ? matchProductName.Value : string.Empty;

                    string entryCSV = matchCatalog.Value + "," + matchCas.Value + "," + name;

                    int count = Math.Min(matchesForPrice.Count, matchesForQuantity.Count);

                    for (int i = 0; i < count; i++)
                    {
                        entryCSV += "," + matchesForPrice[i].Value.Replace(",", "") + "/" + matchesForQuantity[i].Value;
                    }

                    entryCSV += "\n";
                    try
                    {
                        rwl.AcquireWriterLock(100);
                        try
                        {
                            //Write To File
                            File.AppendAllText(filePathCSV, entryCSV);
                        }
                        finally
                        {
                            // Ensure that the lock is released.
                            rwl.ReleaseWriterLock();
                        }
                    }
                    catch (ApplicationException)
                    {
                        // The writer lock request timed out.
                        throw;
                    }

                }
            }
        }



    }
}