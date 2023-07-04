using slskd;
using Soulseek;
using Directory = System.IO.Directory;

var username = "";
var password = "";
var searchText = "";
var saveDirectory = "/home/xxx/Musique/TEMP/";

SoulseekClient client = await Connect(username, password);

var results = await Search(client, searchText);

if(results.Responses.Count>0) {
    if(results.Responses.First().Files.Count>0) {
        Console.WriteLine("Downloading first file of first result.");
        await Download(
            client, 
            username: results.Responses.First().Username, 
            file: results.Responses.First().Files.First(), 
            saveDirectory);
    }
}



void DisplayState(Transfer transfer)
{
    Console.WriteLine(transfer.ToJson());
}

Stream GetLocalFileStream(string remoteFilename, string saveDirectory)
{
    var localFilename = remoteFilename.ToLocalFilename(baseDirectory: saveDirectory);
    var path = Path.GetDirectoryName(localFilename);

    if (!Directory.Exists(path))
    {
        Directory.CreateDirectory(path!);
    }

    return new FileStream(localFilename, FileMode.Create);
}

async Task Download(SoulseekClient client, string username, Soulseek.File file, string saveDirectory)
{
    Console.WriteLine($"Downloading {file.Filename} from {username}");
    var cts = new CancellationTokenSource();
    var topts = new TransferOptions(
                disposeOutputStreamOnCompletion: true,
                stateChanged: (args) =>
                {
                    Console.WriteLine($"State changed from {args.PreviousState} to {args.Transfer.State}");
                    DisplayState(args.Transfer);

                },
                progressUpdated: (args) =>
                {
                    Console.WriteLine("Receiving");
                    DisplayState(args.Transfer);
                });

    var completedTransfer = await client.DownloadAsync(
                                        username: username,
                                        remoteFilename: file.Filename,
                                        outputStreamFactory: () => Task.FromResult(GetLocalFileStream(file.Filename, saveDirectory)),
                                        size: file.Size,
                                        startOffset: 0,
                                        token: null,
                                        cancellationToken: cts.Token,
                                        options: topts);
}

static async Task<(Search Search, IReadOnlyCollection<SearchResponse> Responses)> Search(SoulseekClient client, string searchText)
{
    Console.WriteLine("Searching");
    var searchQuery = SearchQuery.FromText(searchText);
    try
    {
        var responses = await client.SearchAsync(searchQuery);
        Console.WriteLine($"Found results: {responses.Responses.Count}");
        Console.WriteLine(responses.ToJson());
        return responses;
    }
    catch (System.InvalidOperationException ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        return (new Search(searchText, -1, SearchStates.Errored, 0, 0, 0), Array.Empty<SearchResponse>());
    }
}

static async Task<SoulseekClient> Connect(string username, string password)
{
    var client = new SoulseekClient();
    Console.WriteLine("Connecting to Soulseek");
    try
    {
        await client.ConnectAsync(username, password);
        Console.WriteLine("Connected to Soulseek");
    }
    catch (Soulseek.SoulseekClientException ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
    } 
    return client;
}