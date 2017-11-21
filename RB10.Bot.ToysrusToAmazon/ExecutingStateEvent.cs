using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RB10.Bot.ToysrusToAmazon
{
    class ExecutingStateEvent
    {
        public enum NotifyStatus
        {
            Information,
            Warning,
            Error,
            Exception
        }

        public class ExecutingStateEventArgs : EventArgs
        {
            public string ExecDate { get; set; }
            public string Message { get; set; }
            public NotifyStatus NotifyStatus { get; set; }
        }

        public delegate void ExecutingStateEventHandler(object sender, ExecutingStateEventArgs e);
        public event ExecutingStateEventHandler ExecutingStateChanged;

        public void Notify(string message, NotifyStatus reportState)
        {
            if (ExecutingStateChanged != null)
            {
                var eventArgs = new ExecutingStateEventArgs()
                {
                    ExecDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                    Message = message,
                    NotifyStatus = reportState,
                };
                ExecutingStateChanged.Invoke(this, eventArgs);
            }
        }

        protected void Scraping_ExecutingStateChanged(object sender, ExecutingStateEventArgs e)
        {
            if (ExecutingStateChanged != null)
            {
                ExecutingStateChanged.Invoke(this, e);
            }
        }
    }
}
