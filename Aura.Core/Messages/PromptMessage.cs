using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Aura.Messages
{
	internal class PromptMessage
	{
		public PromptMessage (string title, string message, string positiveAction)
		{
			if (string.IsNullOrWhiteSpace (title))
				throw new ArgumentException ("message", nameof (title));
			if (string.IsNullOrWhiteSpace (message))
				throw new ArgumentException ("message", nameof (message));
			if (string.IsNullOrWhiteSpace (positiveAction))
				throw new ArgumentException ("message", nameof (positiveAction));

			Title = title;
			Message = message;
			PositiveAction = positiveAction;
		}

		public Task<bool> Result
		{
			get;
			set;
		}

		public string Title { get; }

		public string Message { get; }
		public string PositiveAction { get; }
	}
}
