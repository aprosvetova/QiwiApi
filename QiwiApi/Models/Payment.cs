using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QiwiApi.Converters;
using QiwiApi.Models.Enums;

namespace QiwiApi.Models {
	public class Payment {
		[JsonProperty("txnId")]
		public long Id { get; set; }

		public DateTime Date { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public PaymentStatus Status { get; set; }

		[JsonConverter(typeof(StringEnumConverter))]
		public PaymentType Type { get; set; }

		public string Account { get; set; }

		[JsonConverter(typeof(SumJsonConverter))]
		public decimal Sum { get; set; }

		[JsonConverter(typeof(SumJsonConverter))]
		public decimal Comission { get; set; }

		[JsonConverter(typeof(SumJsonConverter))]
		public decimal Total { get; set; }

		public string Comment { get; set; }
	}
}