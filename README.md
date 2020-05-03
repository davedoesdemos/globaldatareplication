# Global Data Replication

**Produced by Dave Lusty**

## Introduction

This demo came from a customer problem where large quantities of data must be ingested and replicated to other sites around the world with low latency. Filtering of data is also a requirement to deliver to different customers downstream. The data arrives as a flat text file with tab separated values. As you'll recall I created a [recent demo](https://github.com/davedoesdemos/CSVBlobToEventHub/blob/master/README.md) which ingested CSV files to Event Hubs. This will allow us to do the global distribution before hitting the databases, ensuring the lowest possible latency in all locations. For this I also considered copying the flat files to many locations, but that would add latency since the file copy would need to complete before the first event is fired. With the architecture below we start making events immediately, so individual events are sent globally, each taking milliseconds where the file copy may have taken seconds before starting. In theory, this could also be mixed up with [this demo](https://github.com/davedoesdemos/dataupload) to allow people to drop CSV files into a web interface and turn them into an event stream.

### Architecture

The basic architecture for the demo is shown below. Files will be copied into a storage account (either Blob or ADLS Gen 2). This will then trigger a function app (the one from the previous demo) which will either use a blob trigger (simpler) or an EventGrid trigger (more complex, but shorter start time). The function app will open the file and turn it into JSON events line by line to be sent into Event Hubs. A Stream Analytics job is then used as a consumer of the Event Grid to filter events and send them to a SQL database.

[Basic Architecture](images/BasicArchitecture.png)

Moving this to a multi-tenant architecture is very straightforward. Extra Stream Analytics jobs can be configured with their own consumer groups in Event Hubs. This allows each one to track its position in the data individually, and to separate the work of filtering and delivery. As such, the query used in Stream Analytics is customer specific, allowing their data to be delivered to multiple destinations or to include just the data they need. In terms of global distribution, there is a choice as to whether Stream Analytics is in the source or destination region. I couldn't see a good reason either would be preferred, although since there is a filter operation there will be less data transfer after the job, making it slightly cheaper and more efficient to host in the source region. Similarly I couldn't see any large benefit/drawback when comparing one job delivering to two databases or two jobs for two databases running in parallel. In theory two jobs is better as it allows one database to gracefully fail and recover.

[Multi Tenant](images/MultiTenant.png)

### Data

The data used for this demo came from NASDAQ. I chose their top few stock data sets and added a column for the symbol (which wasn't in the original data) just so we can later filter by that. Obviously your data would normally include the fields you're going to filter on. I could have achieved the same by placing the files into individual folders and adding the stock symbol using the function app code before submission, but this seemed easier since it's not really core to the demo.

```
Symbol,Date, Close/Last, Volume, Open, High, Low
AMD,04/24/2020, $56.18,72854750, $55.1, $56.78, $54.42
AMD,04/23/2020, $55.9,69662690, $56.65, $57.285, $55.64
AMD,04/22/2020, $55.92,63164080, $54.91, $56.15, $54.34
AMD,04/21/2020, $52.92,123906400, $56.9, $57.73, $51.41
AMD,04/20/2020, $56.97,72366360, $55.98, $58.63, $55.85
AMD,04/17/2020, $56.6,76908780, $57.35, $57.755, $55.55
AMD,04/16/2020, $56.95,103106500, $55.96, $58.08, $55.63
AMD,04/15/2020, $54.99,83814030, $53.73, $55.5699, $53.41
```

## Demo

### Infrastructure

For this demo, deploy the function app as per the previous demo, along with the Event Hub. In addition to this, you'll need two Stream Analytics Jobs in the same region, and two Azure SQL Databases with one in the source region and one in any other region around the world.

### Stream Analytics Job


```sql
with FBData as (
  SELECT Volume, PartitionId, System.Timestamp() Date FROM eventhub1
)

Select *

INTO
    EastUSSQL
FROM
    FBData

Select *

INTO
    WestUSSQL
FROM
    FBData
```