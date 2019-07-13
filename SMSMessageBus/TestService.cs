using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SMSMessageBus
{
    public class TestService
    {
        //string connectionString = ConfigurationManager.AppSettings["ServiceBusConnectionString"].ToString();
        string topicName = "notificationtopic";
        BlockingCollection<int> entityIds = new BlockingCollection<int>();
        BlockingCollection<int> empIds = new BlockingCollection<int>();
        SubscriptionClient[] registeredClients = new SubscriptionClient[10];
        static bool RefreshAll = true;
        public async Task Execute()
        {

            var properties = new Dictionary<string, string>
            {
                {"servicebusNamespace", ConfigurationManager.AppSettings["ServiceBusNamespace"].ToString()},
                {"servicebusManageKey", ConfigurationManager.AppSettings["ServiceBusSharedAccessKey"].ToString()}
            };

            //Create Messages for each entity and push them to Service Bus


            var hostName = properties["servicebusNamespace"] ;
            var rootUri = new UriBuilder(Uri.UriSchemeHttp, hostName, -1, "/").ToString();
            var sbUri = new UriBuilder("sb", hostName, -1, "/").ToString();

            var manageToken =
                    TokenProvider.CreateSharedAccessSignatureTokenProvider(
                        "RootManageSharedAccessKey",
                        properties["servicebusManageKey"])
                        .GetWebTokenAsync(rootUri, string.Empty, true, TimeSpan.FromHours(1)).GetAwaiter().GetResult();


            //BlockingCollection<int> entityIds = new BlockingCollection<int>();
            Parallel.For(0, 200, i => { entityIds.Add(i); });
            entityIds.CompleteAdding();

            //BlockingCollection<int> empIds = new BlockingCollection<int>();
            Parallel.For(1000, 1500, i => { empIds.Add(i); });
            empIds.CompleteAdding();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(manageToken);
            var namespaceManager = new NamespaceManager(sbUri, tokenProvider);


            var topicDescription = new TopicDescription(topicName);

            if (RefreshAll)
            {
                Console.WriteLine("Creating Topic....");
                if (namespaceManager.TopicExists(topicDescription.Path))
                {
                    namespaceManager.DeleteTopic(topicDescription.Path);
                }
                await namespaceManager.CreateTopicAsync(topicDescription);
                Console.WriteLine("Created Topic");

            }
            var messagingFactory = MessagingFactory.Create(sbUri, tokenProvider);
            Console.WriteLine("Creating Subscriptions");
            Parallel.ForEach(entityIds, entityID => {
                if (RefreshAll)
                {
                    var subscription = namespaceManager.CreateSubscription(
                    new SubscriptionDescription(topicName, "Entity_" + entityID.ToString()),
                    new RuleDescription(new SqlFilter(string.Format("EntityId = {0}", entityID))));
                    subscription.EnableBatchedOperations = false;
                    subscription.Status = EntityStatus.Active;
                    subscription.LockDuration = TimeSpan.FromSeconds(30);
                    namespaceManager.UpdateSubscription(subscription);
                    Console.WriteLine(string.Format("EntityId = {0} Subscription Created", entityID));
                }
                var subscriptionclient = messagingFactory.CreateSubscriptionClient(topicName, "Entity_" + entityID.ToString(), ReceiveMode.PeekLock);
                this.InitializeReceiver(subscriptionclient, ConsoleColor.Green);
            });
            Console.WriteLine("Starting Sending and Receiving Messages");
            await this.SendMessagesAsync(sbUri, tokenProvider);

        }

        async Task SendMessagesAsync(string namespaceAddress, TokenProvider tokenProvider)
        {
            while (true)
            {
                var messagingFactory = MessagingFactory.Create(namespaceAddress, tokenProvider);
                var sendClient = messagingFactory.CreateTopicClient(topicName);
                var id = Guid.NewGuid().ToString();

                string entityId = entityIds.ToList().PickRandom().ToString();
                string empid = empIds.ToList().PickRandom().ToString();
                //if (Convert.ToInt16(entityId) < 5) continue;
                var notification = new Notification()
                {
                    ID = id,
                    DType = 0,
                    DToken = Guid.NewGuid().ToString(),
                    CreatedOn = DateTime.Now,
                    State = NotificationStatus.Created,
                    UserID = "12",
                    Message = string.Format("This message is sent from entity {0} and emp {1}", entityId, empid),
                    EntityId = entityId,
                    EmployeeID = empid
                    
                };
                TracingHelper.LogMessage(notification);
                var message = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notification))))
                {
                    ContentType = "application/json",
                    Label = "EntityNotification",
                    MessageId = id,
                    TimeToLive = TimeSpan.FromHours(24),
                    Properties =
                    {
                        { "EntityId", Convert.ToInt16(entityId) }
                    }
                };
                //Trace message that it is being queued.
                sendClient.SendAsync(message);
                notification.State = NotificationStatus.Queued;
                TracingHelper.LogMessage(notification); //Queued
                //lock (Console.Out)
                //{
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Message sent: Id = {0} for Entity {1}", message.MessageId, entityId);
                Console.ResetColor();
                //}
                Thread.Sleep(1);
                //Receive(namespaceAddress, tokenProvider);
                //Thread.Sleep(1000);
            }
        }


        void InitializeReceiver(SubscriptionClient receiver, ConsoleColor color)
        {
            Notification notificationObj = null;
            // register the OnMessageAsync callback
            receiver.OnMessageAsync(
                async message =>
                {
                    try
                    {
                        if (message.Label != null &&
                            message.ContentType != null &&
                            message.Label.Equals("EntityNotification", StringComparison.InvariantCultureIgnoreCase) &&
                            message.ContentType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var body = message.GetBody<Stream>();
                            notificationObj = JsonConvert.DeserializeObject<Notification>(new StreamReader(body, true).ReadToEnd());
                            //It is received
                            notificationObj.State = NotificationStatus.Received;
                            TracingHelper.LogMessage(notificationObj);
                            //Send SMS : TODO
                            if (SendSMS(notificationObj))
                            {
                                notificationObj.State = NotificationStatus.Delivered;
                                await message.CompleteAsync();
                                notificationObj.State = NotificationStatus.DeQueued;
                                TracingHelper.LogMessage(notificationObj);
                            }
                            else
                            {
                                notificationObj.State = NotificationStatus.Queued;
                                //Requeue or abandon
                                await message.AbandonAsync();
                                TracingHelper.LogMessage(notificationObj);
                            }
                            Console.ForegroundColor = color;
                            Console.WriteLine(string.Format("Message Received:Entity{0} || Employee:{1}, CreatedOn:{2},Message:{3}", notificationObj.EntityId,
                                notificationObj.EmployeeID, notificationObj.CreatedOn, notificationObj.Message));
                            Console.ResetColor();
                        }
                    }catch(Exception ex)
                    {
                        //Log exception here
                        if(notificationObj != null)
                        {
                            notificationObj.State = NotificationStatus.Exception;
                            TracingHelper.LogMessage(notificationObj);
                        }
                    }

                },
                new OnMessageOptions { AutoComplete = false, MaxConcurrentCalls = 1 });
        }

        private bool SendSMS(Notification notificationObj)
        {
            return true;
        }
    }

}
