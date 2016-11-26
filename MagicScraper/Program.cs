using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace MagicScraper
{
    class Program
    {
        static void Main(string[] args)
        {
            var req = (HttpWebRequest)WebRequest.Create("http://gatherer.wizards.com/Pages/Search/Default.aspx?page=2&output=compact&set=[%22Magic%20Origins%22]");
            string content;
            using (var reader = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                content = reader.ReadToEnd();
            }
            var doc = new HtmlDocument();
            doc.LoadHtml(content);
            var cardItems = doc.DocumentNode.ElementsOfClass("cardItem");
            var items = cardItems.Select(ci => new CardItem(ci));

            using (var writer = new StreamWriter(@"E:\cards.txt"))
            {
                writer.WriteLine("Name\tCost\tType\tPower\tToughness\tRarity");
                foreach (var item in items)
                {
                    writer.WriteLine(item.ToString());
                }
            }

            //content.Display(20);
        }

        enum Rarity
        {
            Common,
            Uncommon,
            Rare,
            Mythic,
            Land
        }

        class CardItem
        {
            public CardItem(HtmlNode node)
            {
                var names = node.ElementsOfClass("name");
                var first = names.First();
                Name = first.Element("a").FirstChild.InnerText;
                
                var costs = node.ElementsOfClass("mana").First().Elements("img").Select(n => n.GetAttributeValue("alt", ""));
                int red = costs.Count(s => s == "Red");
                int green = costs.Count(s => s == "Green");
                int blue = costs.Count(s => s == "Blue");
                int white = costs.Count(s => s == "White");
                int black = costs.Count(s => s == "Black");
                int x = costs.Count(s => s == "Variable Colorless");
                int noColour = costs.Except(new[] { "Red", "Green", "Blue", "White", "Black", "Variable Colorless" })
                    .Sum(s => int.Parse(s));
                Cost = string.Format("{0}{1}{2}{3}{4}{5}{6}",
                    noColour,
                    new string('X', x),
                    new string('R', red),
                    new string('G', green),
                    new string('B', blue),
                    new string('W', white),
                    new string('K', black));

                Type = node.ElementsOfClass("type").First().FirstChild.InnerText.Trim();

                var numericals = node.ElementsOfClass("numerical");
                int power, toughness;
                int.TryParse(numericals.ElementAt(0).FirstChild.InnerText.Trim(), out power);
                int.TryParse(numericals.ElementAt(1).FirstChild.InnerText.Trim(), out toughness);
                Power = power;
                Toughness = toughness;

                string printings = node.ElementsOfClass("printings").First()
                    .Element("div").Element("a").Element("img")
                    .GetAttributeValue("src", "");
                printings = printings.Substring(printings.LastIndexOf('=') + 1);
                switch (printings)
                {
                    case "C":
                        Rarity = Program.Rarity.Common;
                        break;
                    case "R":
                        Rarity = Program.Rarity.Rare;
                        break;
                    case "U":
                        Rarity = Program.Rarity.Uncommon;
                        break;
                    case "M":
                        Rarity = Program.Rarity.Mythic;
                        break;
                    case "L":
                        Rarity = Program.Rarity.Land;
                        break;
                    default:
                        throw new Exception();
                }
            }

            public string Name { get; set; }
            public string Cost { get; set; }
            public string Type { get; set; }
            public int Power { get; set; }
            public int Toughness { get; set; }
            public Rarity Rarity { get; set; }

            public override string ToString()
            {
                return string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}", Name, Cost, Type, Power, Toughness, Rarity);
            }
        }
    }
    
    static class Extensions
    {
        public static IEnumerable<IEnumerable<T>> AsBlocks<T>(this IEnumerable<T> @this, int blockSize)
        {
            for (int i = 0; ; i += blockSize)
            {
                var skipped = @this.Skip(i);
                if (!skipped.Any())
                {
                    yield break;
                }
                yield return skipped.Take(blockSize);
            }
        }

        public static void Display(this string content, int blockSize)
        {
            var lines = content.Split(new[] { "\r\n" }, StringSplitOptions.None);
            int r = 1;
            foreach (var block in lines.AsBlocks(blockSize))
            {
                foreach (var line in block)
                {
                    if (string.IsNullOrEmpty(line))
                    {
                        Console.WriteLine("{0,3}:", r++);
                        continue;
                    }

                    var rows = line.AsBlocks(72).Select(cs => new string(cs.ToArray()));
                    Console.WriteLine("{0,3}:  {1}", r++, rows.First());

                    foreach (var row in rows.Skip(1))
                    {
                        Console.WriteLine("      {0}", row);
                    }
                }

                Console.ReadLine();
            }
        }

        public static IEnumerable<HtmlNode> ElementsOfClass(this HtmlNode node, string className)
        {
            return node.Descendants().Where(e => e.GetAttributeValue("class", "").Contains(className));
        }
    }
}