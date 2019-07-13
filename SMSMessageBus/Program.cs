using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace SMSMessageBus
{
    class Program
    {
        //static string connectionString = ConfigurationManager.AppSettings["ServiceBusConnectionString"].ToString();
        static string topicName = ConfigurationManager.AppSettings["TopicName"].ToString();
        static BlockingCollection<int> entityIds = new BlockingCollection<int>();
        static BlockingCollection<int> empIds = new BlockingCollection<int>();

        static BlockingCollection<SubscriptionClient> clients = new BlockingCollection<SubscriptionClient>();

        static void Main(string[] args)
        {

            TestService service = new TestService();
            service.Execute();


            Console.ReadLine();
        }


    }
      
}
