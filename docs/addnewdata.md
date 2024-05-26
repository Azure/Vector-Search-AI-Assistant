# Uploading New Sample Data

It is possibly to upload custom data to this solution with zero modifications if it is the same retail scenario. The solution has a products container which contains product information, and a customer container which contains a single document for customer profile and multiple salesOrder documents for each of their sales orders.

To upload new data, or to extend the solution to ingest your own data that will be processed by the Change Feed and then made available as a context for chat completions, it's recommended to use the [Cosmos DB Desktop Migration Tool](https://github.com/AzureCosmosDB/data-migration-desktop-tool) to copy your source data into the appropriate container within the deployed instance of Cosmos DB. 

Open a PowerShell and run the following lines to download and extract `dmt.exe`:
```ps
$dmtUrl="https://github.com/AzureCosmosDB/data-migration-desktop-tool/releases/download/2.1.1/dmt-2.1.1-win-x64.zip"
Invoke-WebRequest -Uri $dmtUrl -OutFile dmt.zip
Expand-Archive -Path dmt.zip -DestinationPath .
```

In the folder containing the extracted files, you will see a `migrationsettings.json` file. You will need to edit this file and provide the configuration for the source (e.g., your local files), and the sink (e.g., a container in Cosmos DB).

Here is an example migrationsettings file setup to load a local JSON file, stored in a data folder, to a container in Cosmos DB. Edit this file to suit your needs and save it.

```json
{
  "Source": "JSON",
  "Sink": "Cosmos-nosql",
  "Operations": [
    {
      "SourceSettings": {
        "FilePath": "data\\sampleData.json"
      },
      "SinkSettings": {
        "ConnectionString": "AccountEndpoint=YOUR_CONNECTION_STRING_HERE",
        "Database":"vsai-database",
        "Container":"raw",
        "PartitionKeyPath":"/id",
        "RecreateContainer": false,
        "BatchSize": 100,
        "ConnectionMode": "Direct",
        "MaxRetryCount": 5,
        "InitialRetryDurationMs": 200,
        "UseAutoscaleForCreatedContainer": true,
        "WriteMode": "InsertStream",
        "IsServerlessAccount": false
        }
    }
  ]
}
```

Then run the tool with the following command.

```ps
.\dmt.exe
```

Your new data should now be available in the configured container.


@Matt Gray: Is line valid anymore?
NOTE: If you want to build a reusable, automated script to deploy your files, take a look at the `scripts/Import-Data.ps1` in the source code of this project.