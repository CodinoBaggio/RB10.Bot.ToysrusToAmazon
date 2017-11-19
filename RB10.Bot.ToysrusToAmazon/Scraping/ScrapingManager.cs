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
        public delegate void ExecutingStateEventHandler(object sender, ExecutingStateEventArgs e);
        public event ExecutingStateEventHandler ExecutingStateChanged;

        public void Start(string saveFileName, int toysrusDelay, int amzonDelay)
        {
            Task.Run(() => Run(saveFileName, toysrusDelay, amzonDelay));
        }

        public void Run(string saveFileName, int toysrusDelay, int amzonDelay)
        {
            Notify("処理開始", "処理を開始します。", NotifyStatus.Information, ProcessStatus.End);

            try
            {
                // トイザらスから取得
                var toysrus = new ToysrusScraping { Delay = toysrusDelay };
                toysrus.ExecutingStateChanged += Scraping_ExecutingStateChanged;
                var toysrusResult = toysrus.Run();

                Notify("トイザらス取得完了", "トイザらスからの情報取得が完了しました。", NotifyStatus.Information, ProcessStatus.End);

                // Amazonから取得
                var amazon = new AmazonScraping { Delay = amzonDelay };
                amazon.ExecutingStateChanged += Scraping_ExecutingStateChanged;
                List<ToyInformation> amazonResult = amazon.Run(toysrusResult);

                Notify("Amzon取得完了", "Amzonからの情報取得が完了しました。", NotifyStatus.Information, ProcessStatus.End);

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
            Notify(e);
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

        private void Notify(ExecutingStateEventArgs e)
        {
            if (ExecutingStateChanged != null)
            {
                ExecutingStateChanged.Invoke(this, e);
            }
        }
    }
}
