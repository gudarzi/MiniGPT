using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace MiniGPT
{
	internal class MainViewModel : INotifyPropertyChanged
	{
		public string prompt { get; set; } = "write me a simple prime number checker app in python";

		private string markdown = "";
		public string Markdown
		{
			get
			{
				return markdown;
			}
			set
			{
				markdown = value;
				HTML = $"<head><link href='{prismCss}' rel='stylesheet' /><link href='{customCss}' rel='stylesheet' /></head><body>{MarkdownHtml}</body><script type='text/javascript' src='{prismCore}'></script></script><script></script>";
				NotifyPropertyChanged(nameof(Markdown));
				NotifyPropertyChanged(nameof(HTML));
			}
		}

		private string markdownHtml = "";
		public string MarkdownHtml
		{
			get
			{
				markdownHtml = Markdig.Markdown.ToHtml(Markdown);
				markdownHtml = markdownHtml.Replace("<pre><code>", "<pre><code class='language-*'>"); // trying to beautify general code segments!
				return markdownHtml;
			}
		}

		// Just a random string to avoid conflicting with real websites!
		public string hostName = "JdA9XbYp6zDeGxV7vcKf60WPOyjSaLqtFL1RuQ2kHwEiTsZoCgNnMhU45m3Ir8T";
		public string prismCore => $"http://{hostName}/prism.js";
		public string prismCss => $"http://{hostName}/prism.css";
		public string customCss => $"http://{hostName}/custom.css";

		public string HTML { get; set; }

		public MainViewModel()
		{
			// Just some dummy text!
			Markdown = "Certainly! Here's a Python function that checks if a number is prime or not:\r\n\r\n```python\r\ndef is_prime(num):\r\n    if num < 2:\r\n        return False\r\n    for i in range(2, int(num ** 0.5) + 1):\r\n        if num % i == 0:\r\n            return False\r\n    return True\r\n```\r\n\r\nThis function takes a single argument `num`, which is the number to be checked for primality. It returns `True` if the number is prime, and `False` otherwise. The function first checks if the number is less than 2, in which case it cannot be prime. Then, it iterates over each number from 2 to the square root of `num`, checking if `num` is divisible by any of those numbers. If it is, then it is not prime, so the function returns `False`. If the function completes the loop without finding a divisor, then the number must be prime, and the function returns `True`.";
		}

		public void SendRequest(IProgress<string> progress)
		{
			Markdown = "";
			progress?.Report(HTML);

			double temperature = 0.7;
			string model = "gpt-3.5-turbo";
			string apiKey = "";

			using (HttpClient client = new HttpClient())
			{
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

				var requestContent = new
				{
					model,
					messages = new[] {
														new {
																role = "user",
																content = prompt
																}
														},
					temperature,
					stream = true
				};

				var requestBody = new StringContent(JsonConvert.SerializeObject(requestContent), Encoding.UTF8, "application/json");

				try
				{
					using (var response = client.PostAsync("https://api.openai.com/v1/chat/completions", requestBody))
					{
						if (response.Result.IsSuccessStatusCode)
						{
							using var responseStream = response.Result.Content.ReadAsStreamAsync();
							using var streamReader = new StreamReader(responseStream.Result);
							while (!streamReader.EndOfStream)
							{
								string? line = streamReader.ReadLineAsync().Result;
								if (line != null && line != " ")
								{
									if (line.StartsWith("data: ")) line = line.Substring(6);

									dynamic? data = JsonConvert.DeserializeObject<dynamic>(line);
									if (data?.choices?.Count > 0)
									{
										string message = data.choices[0].delta.content;
										if (message != null)
										{
											//Debug.WriteLine(message); // This is the actual response, word by word!

											Markdown += message;
											Debug.WriteLine(Markdown);

											Thread.Sleep(100);
											progress?.Report(HTML);
										}
									}
								}
							}

							Debug.WriteLine("Completed!");
						}
						else
						{
							Debug.WriteLine($"Error response: {response.Result.Content.ReadAsStringAsync().Result}");
						}
					}

					// This is another way of sending request that returns the whole answer instantly!
					//using (var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", requestBody))
					//{
					//	if (response.IsSuccessStatusCode)
					//	{
					//		var jsonResponse = await response.Content.ReadAsStringAsync();
					//		dynamic? data = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
					//		if (data is null) return;
					//		string completion = data.choices.First().message.content;
					//		Debug.WriteLine($"Completion: {completion}");
					//		Debug.WriteLine(jsonResponse);
					//	}
					//	else
					//	{
					//		Debug.WriteLine($"Error response: {response.Content.ReadAsStringAsync().Result}");
					//	}
					//}
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex);
				}
			}
		}

		#region INPC Implementation
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}
