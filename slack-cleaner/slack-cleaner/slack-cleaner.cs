using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;

public class Product
{
	public string Id { get; set; }
	public string Name { get; set; }
	public decimal Price { get; set; }
	public string Category { get; set; }
}

public class DelReq
{
	public string token { get; set; }
	public string channel { get; set; }
	public string ts { get; set; }
}

public class DelRes
{
	public bool ok { get; set; }
	public string channel { get; set; }
	public string ts { get; set; }
	public string error { get; set; }
}
public class MsgHistroyRes
{
	public bool ok { get; set; }
	public List<MsgRes> messages { get; set; }
}

public class MsgRes
{
	public string client_msg_id { get; set; }
	public string type { get; set; }
	public string text { get; set; }
	public string user { get; set; }
	public string ts { get; set; }

}

class Program
{
	static HttpClient client = new HttpClient();

	static void ShowProduct(Product product)
	{
		Console.WriteLine($"Name: {product.Name}\tPrice: " +
			$"{product.Price}\tCategory: {product.Category}");
	}

	static void ShowRes(MsgHistroyRes res)
	{

		if(res != null)
		{
			res.messages.ForEach(x => {
				Console.WriteLine($"Ts: {x.ts}\tText: {x.text}");
			});
		}
	}

	static void ShowDeleteRes(DelRes res)
	{

		if (res != null)
		{
			Console.WriteLine($"ok: {res.ok}");
		}
	}

	static async Task<Uri> CreateProductAsync(Product product)
	{
		HttpResponseMessage response = await client.PostAsJsonAsync(
			"api/products", product);
		response.EnsureSuccessStatusCode();

		// return URI of the created resource.
		return response.Headers.Location;
	}

	static async Task<MsgHistroyRes> GetMsgHistoryAsync(string path)
	{
		MsgHistroyRes res = null;
		HttpResponseMessage response = await client.GetAsync(path);
		if (response.IsSuccessStatusCode)
		{
			res = await response.Content.ReadAsAsync<MsgHistroyRes>();
		}
		return res;
	}

	static async Task<DelRes> DeleteMsgAsync(string path)
	{
		DelRes res = null;
		HttpResponseMessage response = await client.GetAsync(path);
		if (response.IsSuccessStatusCode)
		{
			res = await response.Content.ReadAsAsync<DelRes>();
		}
		return res;
	}

	static async Task<Product> UpdateProductAsync(Product product)
	{
		HttpResponseMessage response = await client.PutAsJsonAsync(
			$"api/products/{product.Id}", product);
		response.EnsureSuccessStatusCode();

		// Deserialize the updated product from the response body.
		product = await response.Content.ReadAsAsync<Product>();
		return product;
	}

	static async Task<HttpStatusCode> DeleteProductAsync(string id)
	{
		HttpResponseMessage response = await client.DeleteAsync(
			$"api/products/{id}");
		return response.StatusCode;
	}

	/// <summary>
	///  args[0]:token
	///  args[1]:channel
	///  args[2]:all search count
	///  args[3]:userId
	/// </summary>
	/// <param name="args"></param>
	static void Main(string[] args)
	{
		string token = args[0];
		string channel = args[1];
		string count = args[2];
		string userId = args[3];

		RunAsync(token, channel, count, userId).GetAwaiter().GetResult();
	}

	static async Task RunAsync(string token, string channel, string count, string userId)
	{

		// Update port # in the following line.
		client.BaseAddress = new Uri("https://slack.com/");
		client.DefaultRequestHeaders.Accept.Clear();
		client.DefaultRequestHeaders.Accept.Add(
			new MediaTypeWithQualityHeaderValue("application/json"));




		try
		{
			MsgHistroyRes res = new MsgHistroyRes();
			List<Task<DelRes>> tasks = new List<Task<DelRes>>();
			var delResList = new List<DelRes>();
			//Task<List<DelRes>> delRes;


				
			res = await GetMsgHistoryAsync("api/channels.history?token=" + token + "&channel=" + channel +"&count=" + count);

			// private 채널은 groups.history API를 이용해서 삭제해야함.
			if (res.ok.Equals(false))
			{
				res = await GetMsgHistoryAsync("api/groups.history?token=" + token + "&channel=" + channel + "&count=" + count);
			}

			var resMsgList = res.messages.Where(x => x.user == userId).ToList();

			//Console.WriteLine("Count : {0}", resMsgList.Count);

			foreach (var resMsg in resMsgList)
			{
				tasks.Add(Task.Run(() => DeleteMsgAsync("api/chat.delete?token=" + token + "&channel=" + channel + "&ts=" + resMsg.ts)));
			}

			Task.WaitAll(tasks.ToArray());
			foreach (Task t in tasks)
				Console.WriteLine("Task {0} Status: {1}", t.Id, t.Status);


		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
		}

		Console.ReadLine();
	}
}