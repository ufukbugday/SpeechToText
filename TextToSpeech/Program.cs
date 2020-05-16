using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace TextToSpeech
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var client = new HttpClient())
			{
				var bearerToken = GetBearerToken(client);
				Console.WriteLine("İstediğini yaz: ");
				var text = Console.ReadLine();
				var ssml = HandleText(text);
				HandleAudio(ssml, bearerToken, client);
			}
		}

		private static void HandleAudio(string ssml, string bearerToken, HttpClient client)
		{
			var audioRequest = new HttpRequestMessage(HttpMethod.Post,
				"https://eastus.tts.speech.microsoft.com/cognitiveservices/v1");

			audioRequest.Content = new StringContent(ssml);
			audioRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/ssml+xml");
			audioRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerToken);
			audioRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("HorizonSolutions", "1.0"));
			audioRequest.Headers.Add("X-Microsoft-OutputFormat", "audio-24khz-48kbitrate-mono-mp3");

			var audioResult = client.SendAsync(audioRequest).Result;

			using (var fs = File.Open("speech.mpga", FileMode.Create))
			{
				audioResult.Content.ReadAsStreamAsync().Result.CopyTo(fs);
			}
		}

		private static string HandleText(string text)
		{
			var ssml = "<speak version='1.0' xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang='tr-TR'>" +
			           "<voice name='Microsoft Server Speech Text to Speech Voice (tr-TR, SedaRUS)'>"
			           + text +
			           "</voice> </speak>";
			return ssml;
		}

		private static string GetBearerToken(HttpClient client)
		{
			var authRequest =
				new HttpRequestMessage(HttpMethod.Post,
					"https://eastus.api.cognitive.microsoft.com/sts/v1.0/issuetoken");

			authRequest.Headers.Add("Ocp-Apim-Subscription-Key", "{{Your-Key}}");

			var authResult = client.SendAsync(authRequest).Result;

			var bearerToken = "Bearer " + authResult.Content.ReadAsStringAsync().Result;
			return bearerToken;
		}
	}
}
