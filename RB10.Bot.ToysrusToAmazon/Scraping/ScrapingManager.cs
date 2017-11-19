using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RB10.Bot.ToysrusToAmazon.Scraping.AmazonScraping;

namespace RB10.Bot.ToysrusToAmazon.Scraping
{
    class ScrapingManager
    {
        public void Start(string saveFileName, int delay)
        {
            Task.Run(() => Run(saveFileName, delay));
        }

        public void Run(string saveFileName, int delay)
        {
            // トイザらスから取得
            var toysrus = new ToysrusScraping { Delay = delay };
            var toysrusResult = toysrus.Run();

            // Amazonから取得
            var amazon = new AmazonScraping { Delay = delay };
            List<ToyInformation>  amazonResult = amazon.Run(toysrusResult);

            // ファイル出力
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("JANコード,商品名,税込価格,オンライン在庫,店舗在庫あり,店舗在庫わずか,商品画像URL");
            foreach (var result in amazonResult)
            {
                sb.AppendLine($"{result.ToyName},{result.ToysrusPrice},{result.Asin},{result.AmazonPrice}");
            }

            if (0 < amazonResult.Count())
            {
                System.IO.File.WriteAllText(saveFileName, sb.ToString(), Encoding.GetEncoding("shift-jis"));
            }
        }
    }
}
