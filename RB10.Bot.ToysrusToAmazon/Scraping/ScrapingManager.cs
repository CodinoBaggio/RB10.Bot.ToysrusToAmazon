using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RB10.Bot.ToysrusToAmazon.ExecutingStateEvent;
using static RB10.Bot.ToysrusToAmazon.Scraping.AmazonScraping;

namespace RB10.Bot.ToysrusToAmazon.Scraping
{
    class ScrapingManager
    {
        public class Parameters
        {
            public string SaveFileName { get; set; }
            public int ToysrusDelay { get; set; }
            public int AmazonDelay { get; set; }
            public string SearchKeyword { get; set; }
            public List<string> TargetUrls { get; set; }
            public Parameters() => TargetUrls = new List<string>();
        }

        public delegate void ExecutingStateEventHandler(object sender, ExecutingStateEventArgs e);
        public event ExecutingStateEventHandler ExecutingStateChanged;

        public void Start(Parameters parameters)
        {
            Task.Run(() => Run(parameters));
        }

        public void Run(Parameters parameters)
        {
            Notify("処理開始", "処理を開始します。", NotifyStatus.Information, ProcessStatus.End);

            try
            {
                // トイザらスから取得
                var toysrus = new ToysrusScraping { Delay = parameters.ToysrusDelay };
                toysrus.ExecutingStateChanged += Scraping_ExecutingStateChanged;
                var toysrusResult = toysrus.Run(parameters.TargetUrls, parameters.SearchKeyword);

                Notify("トイザらス取得完了", "トイザらスからの情報取得が完了しました。", NotifyStatus.Information, ProcessStatus.End);

                // Amazonから取得
                var amazon = new AmazonScraping { Delay = parameters.AmazonDelay };
                amazon.ExecutingStateChanged += Scraping_ExecutingStateChanged;
                List<ToyInformation> amazonResult = amazon.Run(toysrusResult);

                Notify("Amazon取得完了", "Amazonからの情報取得が完了しました。", NotifyStatus.Information, ProcessStatus.End);

                // ファイル出力
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("トイザらスの商品ページのURL,トイザらスの商品名,トイザらスの税込価格,トイザらスのオンライン在庫,トイザらスの店舗在庫あり,トイザらスの店舗在庫わずか,トイザらスの商品画像URL,Asin,Amazonの税込価格,Amazonの商品画像のURL");
                foreach (var result in amazonResult)
                {
                    sb.AppendLine($"{result.Url},{result.ToyName},{result.Price},{result.OnlineStock},{result.StoreStockCount},{result.StoreLessStockCount},{result.ImageUrl},{result.Asin},{result.AmazonPrice},{result.AmazonImageUrl}");
                }

                if (0 < amazonResult.Count())
                {
                    System.IO.File.WriteAllText(parameters.SaveFileName, sb.ToString(), Encoding.GetEncoding("shift-jis"));

                    Notify("結果ファイル出力完了", "結果ファイルの出力が完了しました。", NotifyStatus.Information, ProcessStatus.End);
                }
            }
            catch (Exception ex)
            {
                Notify("例外エラー", ex.ToString(), NotifyStatus.Exception, ProcessStatus.End);
            }
            finally
            {
                Notify("処理完了", "すべての処理が完了しました。", NotifyStatus.Information, ProcessStatus.End);
            }
        }

        private void Scraping_ExecutingStateChanged(object sender, ExecutingStateEventArgs e)
        {
            if (ExecutingStateChanged != null)
            {
                ExecutingStateChanged.Invoke(this, e);
            }
        }

        protected void Notify(string info, string message, NotifyStatus reportState, ProcessStatus processState = ProcessStatus.Start)
        {
            if (ExecutingStateChanged != null)
            {
                var eventArgs = new ExecutingStateEventArgs()
                {
                    Info = info,
                    Message = message,
                    NotifyStatus = reportState,
                    ProcessStatus = processState
                };
                ExecutingStateChanged.Invoke(this, eventArgs);
            }
        }
    }
}
