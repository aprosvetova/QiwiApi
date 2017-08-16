using System;
using QiwiApi.Models;

namespace QiwiApi.Events {
	public class PaymentEventArgs : EventArgs {
		public Payment Payment { get; private set; }

		internal PaymentEventArgs(Payment payment) {
			Payment = payment;
		}
	}
}
