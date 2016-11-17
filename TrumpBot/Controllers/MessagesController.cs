using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace TrumpBot
{
	[BotAuthentication]
	public class MessagesController : ApiController
	{
		/// <summary>
		/// POST: api/Messages
		/// Receive a message from a user and reply to it
		/// </summary>
		public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
		{
			if (activity.Type == ActivityTypes.Message)
			{
				ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

				// return our reply to the user
				if (activity.Text.IndexOf("Trump", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					string replyText = string.Empty;
					Attachment attachment = null;

					switch (GetUserIntention(activity.Text))
					{
						case UserIntention.GetQuote:
							replyText = await RetrieveQuote();
							break;

						case UserIntention.GetInsult:
							replyText = await RetrieveInsult();
							break;

						case UserIntention.GetFact:
							replyText = await RetrieveTrumpFact();
							break;

						case UserIntention.GetCatFact:
							replyText = await RetrieveCatFact();
							break;

						case UserIntention.GetDance:
							replyText = null;
							attachment = CreateDanceAttachment();
							break;
					}

					if (!string.IsNullOrEmpty(replyText) || attachment != null)
					{
						Activity reply = activity.CreateReply(replyText);
						if (attachment != null)
						{
							reply.Attachments.Add(attachment);
						}
						await connector.Conversations.ReplyToActivityAsync(reply);
					}
				}					
			}
			
			var response = Request.CreateResponse(HttpStatusCode.OK);
			return response;
		}

		private enum UserIntention { GetInsult, GetQuote, GetFact, GetCatFact, GetDance };

		private UserIntention GetUserIntention(string message)
		{
			var intention = UserIntention.GetQuote;

			if (message.IndexOf("fact", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				intention = UserIntention.GetFact;
			}
			else if (message.IndexOf("cat", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				intention = UserIntention.GetCatFact;
			}
			else if (message.IndexOf("dance", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				intention = UserIntention.GetDance;
			}
			else if (message.IndexOf("suck", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("hate", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("incompetent", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("jerk", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("monster", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("unfit", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("dangerous", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("fascist", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("dictator", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("asshole", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("terrible", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("assisinate", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("rapist", StringComparison.OrdinalIgnoreCase) >= 0 ||
					message.IndexOf("fuck", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				intention = UserIntention.GetInsult;
			}

			return intention;
		}

		private async Task<string> RetrieveQuote()
		{
			HttpClient client = new HttpClient();
			Task<string> getStringTask = client.GetStringAsync("https://api.whatdoestrumpthink.com/api/v1/quotes/random");
			var result = await getStringTask;
			var parsed = JsonConvert.DeserializeObject<QuoteResult>(result);
			return parsed.message;

		}

		private async Task<string> RetrieveTrumpFact()
		{
			List<string> ChuckTokens = new List<string>() { "Chuck Norris", "Chuck", "Norris" };
			HttpClient client = new HttpClient();
			Task<string> getStringTask = client.GetStringAsync("https://api.chucknorris.io/jokes/random");
			var result = await getStringTask;
			var parsed = JsonConvert.DeserializeObject<ChuckToTrumpResult>(result);
			var fact = parsed.value;

			foreach (var token in ChuckTokens)
			{
				fact = ReplaceString(fact, token, "Trump", StringComparison.OrdinalIgnoreCase);
			}

			return string.Format("TrumpFact: {0}", fact);
		}


		private async Task<string> RetrieveInsult()
		{
			HttpClient client = new HttpClient();
			Task<string> getStringTask = client.GetStringAsync("http://www.insultgenerator.org/");
			var result = await getStringTask;
			var startTag = "<br><br>";
			var endTag = "</div>";
			int startIndex = result.IndexOf(startTag);
			int endIndex = result.IndexOf(endTag, startIndex);
			return result.Substring(startIndex + startTag.Length, (endIndex - startIndex - (endTag.Length + 2)));
		}

		private async Task<string> RetrieveCatFact()
		{
			HttpClient client = new HttpClient();
			Task<string> getStringTask = client.GetStringAsync("http://catfacts-api.appspot.com/api/facts");
			var result = await getStringTask;
			var parsed = JsonConvert.DeserializeObject<CatFactsResult>(result);
			return string.Format("CatFact: {0}", parsed.facts.FirstOrDefault());
		}

		private Attachment CreateDanceAttachment()
		{
			return new Attachment()
			{
				ContentUrl = "https://m.popkey.co/160d81/k08O5.gif?c=popkey-web&p=popkey&i=hotlinebling&l=direct&f=.gif",
				ContentType = "image/gif"
			};
		}
				
		public static string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison)
		{
			StringBuilder sb = new StringBuilder();

			int previousIndex = 0;
			int index = str.IndexOf(oldValue, comparison);
			while (index != -1)
			{
				sb.Append(str.Substring(previousIndex, index - previousIndex));
				sb.Append(newValue);
				index += oldValue.Length;

				previousIndex = index;
				index = str.IndexOf(oldValue, index, comparison);
			}
			sb.Append(str.Substring(previousIndex));

			return sb.ToString();
		}

		private class ChuckToTrumpResult
		{
			public string value { get; set; }
			public string category { get; set; }
			public string icon_url { get; set; }
			public string url { get; set; }
			public string id { get; set; }
		}

		private class QuoteResult
		{
			public string message { get; set; }
		}

		private class CatFactsResult
		{
			public List<string> facts { get; set; }
		}		
	}
}