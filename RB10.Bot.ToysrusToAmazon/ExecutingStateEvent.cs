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

        public enum ProcessStatus
        {
            Start,
            Processing,
            End
        }

        public class ExecutingStateEventArgs : EventArgs
        {
            public string Info { get; set; }
            public string Message { get; set; }
            public NotifyStatus NotifyStatus { get; set; }
            public ProcessStatus ProcessStatus { get; set; }
        }
    }
}
