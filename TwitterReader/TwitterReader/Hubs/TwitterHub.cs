using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace TwitterReader.Hubs
{
    public class TwitterHub : Hub
    {
        private static IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>();

        // Call this from JS: hub.client.send()
        public void Send()
        {
            Clients.All.addNewMessageToPage();
        }

        // Call this from C#: TwitterHub.Static_Send()
        public static void Static_Send()
        {
            hubContext.Clients.All.addNewMessageToPage();
        }
    }
}