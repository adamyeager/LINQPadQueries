<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.FileSystem.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.IO.Compression.dll</Reference>
  <NuGetReference>HtmlAgilityPack</NuGetReference>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>HtmlAgilityPack</Namespace>
  <Namespace>System.IO.Compression</Namespace>
</Query>

/// ********************************************************************************
/// *
/// * Attack On Curl is a simple app that allows you to download images from a  
/// * website in a numbered sequence, and then zip up those images and change the 
/// * file extension from ZIP to CBZ.
/// *
/// ********************************************************************************



string baseUrl = "http://www.mangareader.net";
string baseFileLocation = @"E:\Comics\attack-on-titan";
string baseUrlPath = "shingeki-no-kyojin";
string outputFileNameFormat = "Attack On Titan";
int startNumber = 92;
int endNumber = 97;

void Main()
{
	for (int i = startNumber; i <= endNumber; i++)
	{
		string issueNumberString = i.ToString();
		string issueNumber = issueNumberString.ToString().PadLeft(3, '0');

		string currentSaveLocation = string.Format("{0}\\{1}", baseFileLocation, issueNumber);
		Directory.CreateDirectory(currentSaveLocation);
		string nextPage = string.Format("{0}/{1}/{2}", baseUrl, baseUrlPath, i);
		int pageNumber = 1;

		List<string> childPages = new List<string>();

		var request = (HttpWebRequest)WebRequest.Create(nextPage);
		request.UserAgent = "Mozilla/5.0";

		using (var response = request.GetResponse())
		{
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				Console.WriteLine(string.Format("Reqeusting {0}", nextPage));

				string html = reader.ReadToEnd();
				Console.WriteLine("HTML received!");

				HtmlNode bodyNode = GetHtmlBodyNode(html);

				//save first page
				string imageUrl = GetImageUrl(bodyNode);
				SavePage(imageUrl, currentSaveLocation, pageNumber);

				//System.Threading.Thread.Sleep(waitTime);
				pageNumber++;
				Console.WriteLine("");

				childPages = GetPagesFromSelectHtmlElement(bodyNode);
			}
		}

		List<KeyValuePair<int, string>> numberedChildPages = new List<KeyValuePair<int, string>>();
		foreach (var page in childPages)
		{
			numberedChildPages.Add(new KeyValuePair<int, string>(pageNumber, page));
			pageNumber++;
		}

		Parallel.ForEach(numberedChildPages, kvp =>
			{
				GetAndSaveChildPage(kvp.Value, currentSaveLocation, kvp.Key);
			});

		Console.WriteLine("");
		Console.WriteLine("Zipping up the directory to build the CBZ file...");

		BuildCBZFile(currentSaveLocation, issueNumber);

		Console.WriteLine("CBZ file created!");

		Console.WriteLine("Removing download directory...");
		System.IO.Directory.Delete(currentSaveLocation, true);
		Console.WriteLine("Directory deleted!");

		Console.WriteLine("");
		Console.WriteLine("-----------------------");
		Console.WriteLine(string.Format("Issue #{0} complete!", i));
		Console.WriteLine("-----------------------");
		Console.WriteLine("");
	}
}


private void BuildCBZFile(string location, string issueNumber)
{
	string destinationFile = string.Format("{0}\\{1} {2}.cbz", baseFileLocation, outputFileNameFormat, issueNumber);
	ZipFile.CreateFromDirectory(location, destinationFile, CompressionLevel.Optimal, true);
}


private void GetAndSaveChildPage(string pageUrl, string saveLocation, int pageNumber)
{
	var request = WebRequest.Create(pageUrl);

	using (var response = request.GetResponse())
	{
		using (var reader = new StreamReader(response.GetResponseStream()))
		{
			Console.WriteLine(string.Format("Reqeusting {0}", pageUrl));

			string html = reader.ReadToEnd();
			Console.WriteLine("HTML received!");

			HtmlNode bodyNode = GetHtmlBodyNode(html);

			string imageUrl = GetImageUrl(bodyNode);
			SavePage(imageUrl, saveLocation, pageNumber);
		}
	}
}


private void SavePage(string imageUrl, string saveLocation, int pageNumber)
{
	string pageNumberString = pageNumber.ToString();
	string pageName = pageNumber.ToString().PadLeft(3, '0');

	string fileName = string.Format("{0}\\{1}.jpg", saveLocation, pageName);
	Console.WriteLine(string.Format("Attempting to save {0}", fileName));

	var request = WebRequest.Create(imageUrl);

	using (var response = request.GetResponse())
	{
		using (var fileStream = File.Create(fileName))
		{
			response.GetResponseStream().CopyTo(fileStream);
		}
	}

	Console.WriteLine(string.Format("Page {0} saved!", pageNumber));
}


private string GetImageUrl(HtmlNode bodyNode)
{
	if (bodyNode != null)
	{
		var imageElement = bodyNode.SelectNodes("//div[@id='imgholder']//a//img").FirstOrDefault();

		if (imageElement != null)
		{
			if (imageElement.HasAttributes)
			{
				string imageUrl = imageElement.Attributes.Where(o => o.Name == "src").Select(o => o.Value).FirstOrDefault();
				return imageUrl;
			}
		}
	}

	return "";
}


private List<string> GetPagesFromSelectHtmlElement(HtmlNode bodyNode)
{
	List<string> results = new List<string>();

	var options = bodyNode.SelectNodes("//select[@id='pageMenu']//option");

	if (options != null)
	{
		foreach (var option in options)
		{
			if (option.Attributes.Count() < 2)
			{
				results.Add(string.Concat(baseUrl, option.Attributes.Select(o => o.Value).FirstOrDefault()));
			}
		}
	}

	return results;
}


private string GetNextPageFromHtml(HtmlNode bodyNode)
{
	if (bodyNode != null)
	{
		// Do something with bodyNode
		var nextPageHtmlElement = bodyNode.SelectNodes("//div[@id='imgholder']//a").FirstOrDefault();

		if (nextPageHtmlElement != null)
		{
			if (nextPageHtmlElement.HasAttributes)
			{
				string nextPage = nextPageHtmlElement.Attributes.Where(o => o.Name == "href").Select(o => o.Value).FirstOrDefault();

				if (!string.IsNullOrEmpty(nextPage))
				{
					var parts = nextPage.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					if (parts.Count() > 2)
					{
						return nextPage;
					}
				}
			}
		}
	}

	return "";
}


private HtmlNode GetHtmlBodyNode(string html)
{
	HtmlDocument htmlDoc = new HtmlDocument();

	// There are various options, set as needed
	htmlDoc.OptionFixNestedTags = true;

	// filePath is a path to a file containing the html
	htmlDoc.LoadHtml(html);

	if (htmlDoc.DocumentNode != null)
	{
		var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
		return bodyNode;
	}
	else
	{
		return null;
	}
}