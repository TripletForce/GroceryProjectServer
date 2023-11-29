using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReciptServer
{
    public class Receipt
    {
        public string ID;
        public string StoreName = "NULL";
        public string Street = "NULL";
        public string City = "NULL";
        public string State = "NULL";
        public int PostalCode = 0;

        public decimal Total = 0;
        public decimal SubTotal = 0;
        public decimal Tax1 = 0;
        public decimal Tax2 = 0;

        public string PaymentType = "NULL";
        public string PhoneNumber = "NULL";

        public DateTime ReceiptDate;


        public List<PurchasedItem> PurchasedItems = new();
    }
}
