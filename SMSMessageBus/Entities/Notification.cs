using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSMessageBus
{
    public enum NotificationStatus
    {
        Created = 0,
        Queued = 1,
        Received = 2,
        Delivered = 3,
        DeQueued = 4,
        Exception = 5
    }

    [Serializable]
    public class Notification : TableEntity 
    {
        /// <summary>
        /// RowKey
        /// </summary>
        public string ID { get; set; }

        public string UserID { get; set; }

        ///PartitionKey
        /// <summary>
        /// 0->Created
        /// 1->Queued
        /// 2->Received
        /// 3->Delivered
        /// 4->QueueDeleted
        /// </summary>
        public NotificationStatus State { get; set; }

        /// <summary>
        /// 0-Android
        /// 1-Apple
        /// </summary>
        public int DType { get; set; }

        public string DToken { get; set; }


        public DateTime CreatedOn { get; set; }


        public string EntityId { get; set; }

        public string EmployeeID { get; set; }

        public string Header { get; set; }

        public string Message { get; set; }

       

        public string CreatedById { get; set; }


    }

   

}
