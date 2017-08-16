using System;
namespace QiwiApi.Requests {
	public class MoneyTransfer {
		public string Id { get; set; }

		public Sum Sum { get; set; }

		public string Source { get; set; }

		public PaymentMethod PaymentMethod { get; set; }

		public string Comment { get; set; }

		public Fields Fields { get; set; }
	}

	public class Sum {
		public string Amount { get; set; }

		public string Currency { get; set; }
	}

	public class PaymentMethod {
		public string Type { get; set; }

		public string AccountId { get; set; }
	}

	public class Fields {
		public string Account { get; set; }
	}
}
