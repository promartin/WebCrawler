using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;

namespace WebCrawler
{
    class VideoCard
    {
        internal string manufacturer;
        internal string productName;
        internal string inter;
        internal string gbSize;
        internal string slotWidth;
        internal string tdp;
        internal string connectors;
        internal string videocardLength;
        internal List<string> design = new List<string>();
        internal string otherChangesInDesigns;

        public VideoCard(string manufacturer, string productName, string inter, string gbSize, string slotWidth, string tdp, string connectors, string videocardLength, List<string> design, string otherChangesInDesigns)
        {
            this.manufacturer = manufacturer;
            this.productName = productName;
            this.inter = inter;
            this.gbSize = gbSize;
            this.slotWidth = slotWidth;
            this.tdp = tdp;
            this.connectors = connectors;
            this.videocardLength = videocardLength;
            this.design = design;
            this.otherChangesInDesigns = otherChangesInDesigns;
        }
    }

    class Program
    {
        public static List<VideoCard> videoCardList = new List<VideoCard>();

        static async Task Main(string[] args)
        {
            await StartCrawlerAsync();
            Console.Write("\nCrawling is complete!\nJhaaj de kis ügyes vagy, hogy rágjalak meg! :3");
            Console.ReadLine();
        }

        static async Task StartCrawlerAsync()
        {
            string url = "https://www.techpowerup.com/gpu-specs/?mobile=No";
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(url);
            HtmlNode.ElementsFlags["tbody"] = HtmlElementFlag.Closed;
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            
            //First select the field where the filters are
            HtmlNode filterNode = document.DocumentNode.Descendants("fieldset").FirstOrDefault(x => x.GetAttributeValue("class", "").Equals("filters"));

            //Now lets select the manufacturer node from the filter fields
            HtmlNode manufacturerNodes = filterNode.Descendants("select").FirstOrDefault(x => x.GetAttributeValue("id", "").Equals("mfgr"));

            //Now lets check the inner text of all child nodes and save them into a string list
            List<string> manufacturers = new List<string>();
            if (manufacturerNodes.HasChildNodes)
            {
                for (int i = 0; i < manufacturerNodes.ChildNodes.Count; i++)
                {
                    if (SanitizeString(manufacturerNodes.ChildNodes[i].InnerText) != "" && SanitizeString(manufacturerNodes.ChildNodes[i].InnerText) != "All")
                    {
                        manufacturers.Add(manufacturerNodes.ChildNodes[i].InnerText);
                    }
                }
            }
            else
            {
                Console.WriteLine("Nincs child nodeja a cuccnak, szal ez gáz ¯\\_(ツ)_/¯");
            }

            //The next step is to go trough these manufacturers and add the respected parameter to the URL and start to prepare for the data extraction
            for (int i = 0; i < manufacturers.Count; i++)
            {
                if (i == 0)
                {
                    Console.WriteLine($"Manufacturer: {manufacturers[i].Split(' ')[0]}");
                }
                else
                {
                    Console.WriteLine($"\nManufacturer: {manufacturers[i].Split(' ')[0]}");
                }
                url = "https://www.techpowerup.com/gpu-specs/?mobile=No" + "&mfgr=" + manufacturers[i].Split(' ')[0];
                await QueryVideocards(url);
            }
        }

        static async Task QueryVideocards(string urlOfVideocards)
        {
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(urlOfVideocards);
            HtmlNode.ElementsFlags["tbody"] = HtmlElementFlag.Closed;
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            //Now again, we select the field where the filters are
            HtmlNode filterNode = document.DocumentNode.Descendants("fieldset").FirstOrDefault(x => x.GetAttributeValue("class", "").Equals("filters"));

            //Check if every video card fits on the page
            if (document.DocumentNode.Descendants().FirstOrDefault(x => x.InnerText == "Too many results found, first 100 displayed. Please refine search parameters.") != null)
            {
                //If not we refine the search with the released node
                //Select the released node from the filter fields
                HtmlNode releaseNodes = filterNode.Descendants("select").FirstOrDefault(x => x.GetAttributeValue("id", "").Equals("released"));
                List<int> years = new List<int>();

                if (releaseNodes.HasChildNodes)
                {
                    for (int i = 0; i < releaseNodes.ChildNodes.Count; i++)
                    {
                        string year = releaseNodes.ChildNodes[i].InnerText;
                        if (SanitizeString(year) != "" && SanitizeString(year) != "All")
                        {
                            if (int.TryParse(year.Split(' ')[0], out int result))
                            {
                                years.Add(result);
                            }
                        }
                    }
                }

                for (int i = 0; i < years.Count; i++)
                {
                    if (i == 0)
                    {
                        Console.WriteLine($"Year: {years[i]}");
                    }
                    else
                    {
                        Console.WriteLine($"\nYear: {years[i]}");
                    }

                    await GoignTroughVideocardsOnPage(urlOfVideocards + $"&released=" + years[i]);
                }
            }
            else
            {
                //Else if it fits
                await GoignTroughVideocardsOnPage(urlOfVideocards);
            }
        }

        static async Task GoignTroughVideocardsOnPage(string urlOfCards)
        {
            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(urlOfCards);
            HtmlNode.ElementsFlags["tbody"] = HtmlElementFlag.Closed;
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            //Going through the videocards on the page
            HtmlNodeCollection trsNodes = document.DocumentNode.SelectSingleNode("//table[@class='processors']").SelectNodes("//tr");

            string everyUrlOnPage = "";
            string videoCardUrl = "";
            for (int i = 0; i < trsNodes.Count; i++)
            {
                if (trsNodes[i].HasChildNodes)
                {
                    for (int j = 0; j < trsNodes[i].ChildNodes.Count; j++)
                    {
                        if (trsNodes[i].ChildNodes[j].Name == "td")
                        {
                            if (trsNodes[i].ChildNodes[j].HasAttributes && !trsNodes[i].ChildNodes[j].WriteTo().Contains("Edited"))
                            {
                                videoCardUrl = trsNodes[i].ChildNodes[j].ChildNodes[1].GetAttributeValue("href", "") + Environment.NewLine;
                            }

                            if (trsNodes[i].ChildNodes[j].InnerText == DateTime.Now.Year.ToString() || trsNodes[i].ChildNodes[j].InnerText == "Unknown")
                            {
                                videoCardUrl = "";
                            }
                        }
                    }
                }

                if (videoCardUrl != "" && !everyUrlOnPage.Contains(videoCardUrl))
                {
                    everyUrlOnPage += videoCardUrl;
                }
            }

            string[] urlStrings = everyUrlOnPage.Split(Environment.NewLine.ToCharArray());

            for (int i = 0; i < urlStrings.Length; i++)
            {
                if (urlStrings[i] != "")
                {
                    await GetDataOfVideoCardAsync(urlStrings[i]);
                }
            }
        }

        static async Task GetDataOfVideoCardAsync(string videoCardsUrl)
        {
            string manufacturer = "None";
            string productName = "None";
            string inter = "None";
            string gbSize = "None";
            string slotWidth = "None";
            string tdp = "None";
            string connectors = "None";
            string videocardLength = "None";
            List<string> design = new List<string>();
            string otherChangesInDesigns = "None";
            VideoCard newVideoCard;

            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync("https://www.techpowerup.com" + videoCardsUrl);
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);

            string fullName = document.DocumentNode.Descendants("h1").FirstOrDefault().InnerText;
            manufacturer = fullName.Split(' ')[0].Trim();
            productName = fullName.Replace(manufacturer, "").Trim();

            List<HtmlNode> details = document.DocumentNode.Descendants("section").Where(x => x.GetAttributeValue("class", "").Equals("details")).ToList();

            for (int i = 0; i < details.Count; i++)
            {
                if (details[i].Descendants("h2").FirstOrDefault().InnerText == "Graphics Card")
                {
                    List<HtmlNode> dlList = details[i].Descendants("dl").ToList();

                    for (int j = 0; j < dlList.Count; j++)
                    {
                        if (dlList[j].Descendants("dt").FirstOrDefault().InnerText == "Bus Interface")
                        {
                            inter = dlList[j].Descendants("dd").FirstOrDefault().InnerText;
                        }
                    }
                }

                if (details[i].Descendants("h2").FirstOrDefault().InnerText == "Memory")
                {
                    List<HtmlNode> dlList = details[i].Descendants("dl").ToList();

                    for (int j = 0; j < dlList.Count; j++)
                    {
                        if (dlList[j].Descendants("dt").FirstOrDefault().InnerText == "Memory Size")
                        {
                            gbSize = dlList[j].Descendants("dd").FirstOrDefault().InnerText;
                        }
                    }
                }

                if (details[i].Descendants("h2").FirstOrDefault().InnerText == "Board Design")
                {
                    List<HtmlNode> dlList = details[i].Descendants("dl").ToList();

                    for (int j = 0; j < dlList.Count; j++)
                    {
                        if (dlList[j].Descendants("dt").FirstOrDefault().InnerText == "Slot Width")
                        {
                            slotWidth = dlList[j].Descendants("dd").FirstOrDefault().InnerText;
                        }
                        else if (dlList[j].Descendants("dt").FirstOrDefault().InnerText == "Length")
                        {
                            videocardLength = dlList[j].Descendants("dd").FirstOrDefault().InnerText;
                            if (videocardLength.Contains("inches"))
                            {
                                videocardLength = videocardLength.Remove(0, videocardLength.LastIndexOf('s') + 1);
                            }
                        }
                        else if (dlList[j].Descendants("dt").FirstOrDefault().InnerText == "TDP")
                        {
                            tdp = dlList[j].Descendants("dd").FirstOrDefault().InnerText;
                        }
                        else if (dlList[j].Descendants("dt").FirstOrDefault().InnerText == "Power Connectors")
                        {
                            connectors = dlList[j].Descendants("dd").FirstOrDefault().InnerText;
                        }
                    }
                }
            }

            HtmlNode customSectionNode = document.DocumentNode.Descendants("section").FirstOrDefault(x => x.GetAttributeValue("class", "").Equals("details customboards"));
            if (customSectionNode != null)
            {
                List<HtmlNode> customSectionTrNodes = customSectionNode.Descendants("tr").Where(x => x.HasAttributes).ToList();

                for (int i = 0; i < customSectionTrNodes.Count; i++)
                {
                    bool first = false;

                    for (int j = 0; j < customSectionTrNodes[i].ChildNodes.Count; j++)
                    {
                        if (customSectionTrNodes[i].ChildNodes[j].Name == "td" && !first)
                        {
                            design.Add(customSectionTrNodes[i].ChildNodes[j].ChildNodes.FindFirst("a").InnerText);
                            first = true;
                        }

                        if (j == 9 && customSectionTrNodes[i].ChildNodes[j].Name == "td" && customSectionTrNodes[i].ChildNodes[j].InnerText != "")
                        {
                            string a = customSectionTrNodes[i].ChildNodes[j].WriteTo();
                        }
                    }
                }
            }

            bool nincsIlyenVideokartya = true;
            bool kicsiAValtozas = true;
            newVideoCard = new VideoCard(manufacturer, productName, inter, gbSize, slotWidth, tdp, connectors, videocardLength, design, otherChangesInDesigns);
            int listCount = videoCardList.Count;

            if (listCount == 0)
            {
                videoCardList.Add(newVideoCard);
                WriteToCSV();
            }
            else
            {
                for (int i = 0; i < listCount; i++)
                {
                    if (videoCardList[i].productName == productName)
                    {
                        if (videoCardList[i].gbSize != gbSize)
                        {
                            videoCardList.Add(newVideoCard);
                            WriteToCSV();
                            break;
                        }
                        else
                        {
                            kicsiAValtozas = false;
                            break;
                        }
                    }
                    else
                    {
                        videoCardList.Add(newVideoCard);
                        WriteToCSV();
                        break;
                    }
                }
            }
            //Ellenőrizd, hogy van-e már a meglévő listában, és érdemes-e újra berakni
            //Ugyan olyan vidikártyáknál, csak más néven, ha nincs meg a hossza, vegye ki az előzőből.

            void WriteToCSV()
            {
                if (nincsIlyenVideokartya)
                {
                    if (kicsiAValtozas)
                    {
                        using (StreamWriter sw = new StreamWriter("adatok.txt", true))
                        {
                            string adat = $"{SanitizeString(manufacturer)};{productName};{inter};{SanitizeString(gbSize)};{SanitizeString(slotWidth)};{SanitizeString(tdp)};{SanitizeString(connectors)};{SanitizeString(videocardLength)}";

                            if (design.Count > 0)
                            {
                                adat += ";designs";
                                for (int i = 0; i < design.Count; i++)
                                {
                                    adat += $";{design[i]}";
                                }
                            }

                            sw.WriteLine(adat + Environment.NewLine);
                        }

                        Console.WriteLine($"{productName}... Done!");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"{productName}... Small difference!");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{productName}... Already exists!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        

        private static string SanitizeString(string dirtyString)
        {
            return new String(dirtyString.Where(Char.IsLetterOrDigit).ToArray());
        }
    }
}