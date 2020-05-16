using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TextToSpeech
{
	class Program
	{
		public static readonly string TEXT_TRANSLATION_API_ENDPOINT =
			"https://api.cognitive.microsofttranslator.com/{0}?api-version=3.0";
		const string COGNITIVE_SERVICES_KEY = "COGNITIVE_SERVICES_KEY";
		static void Main(string[] args)
		{
			Console.WriteLine("İstediğini yaz: ");
			var text = Console.ReadLine();
			var translation = Translate(text);

			using (var client = new HttpClient())
			{
				var bearerToken = GetBearerToken(client);
				var ssml = HandleText(translation);
				HandleAudio(ssml, bearerToken, client);
			}
		}

		private static string Translate(string text)
		{
			var fromLanguageCode = "tr";
			var toLanguageCode = "de";
			string endpoint = string.Format(TEXT_TRANSLATION_API_ENDPOINT, "translate");
			string uri = string.Format(endpoint + "&from={0}&to={1}", fromLanguageCode, toLanguageCode);
			var body = new object[] {new {Text = text}};
			var requestBody = JsonConvert.SerializeObject(body);
			using (var client = new HttpClient())
			using (var request = new HttpRequestMessage())
			{
				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri(uri);
				request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
				request.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);
				request.Headers.Add("Ocp-Apim-Subscription-Region", "eastus");
				request.Headers.Add("X-ClientTraceId", Guid.NewGuid().ToString());

				var response = client.SendAsync(request).Result;
				var responseBody = response.Content.ReadAsStringAsync().Result;

				var result =
					JsonConvert.DeserializeObject<List<Dictionary<string, List<Dictionary<string, string>>>>>(responseBody);
				var translation = result[0]["translations"][0]["text"];

				// Update the translation field
				return translation;
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
			var ssml = "<speak version='1.0' xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang='de-DE'>" +
					   "<voice name='Microsoft Server Speech Text to Speech Voice (de-DE, KatjaNeural)'>"
					   + text +
			           "</voice> </speak>";
			return ssml;
		}

		private static string GetBearerToken(HttpClient client)
		{
			var authRequest =
				new HttpRequestMessage(HttpMethod.Post,
					"https://eastus.api.cognitive.microsoft.com/sts/v1.0/issuetoken");

			authRequest.Headers.Add("Ocp-Apim-Subscription-Key", COGNITIVE_SERVICES_KEY);

			var authResult = client.SendAsync(authRequest).Result;

			var bearerToken = "Bearer " + authResult.Content.ReadAsStringAsync().Result;
			return bearerToken;
		}
	}
}
