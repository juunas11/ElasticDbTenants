# Using Durable Functions to create databases for tenants in Azure SQL Elastic Pool

This application shows how a Durable Functions workflow can be used to create and delete tenant databases from an Azure SQL Elastic Pool.
There is also a Razor Pages application that uses the Durable Functions.

Projects and their purpose:

- ElasticDbTenants.Db.Common: Common utilities for both the tenant catalog DB and tenant DBs (currently only Azure AD authentication connection interceptor)
- ElasticDbTenants.CatalogDb: Models and EF Core context for the catalog DB that contains information on tenants in the system
- ElasticDbTenants.TenantDb: Models and EF Core context for the tenant DBs
- ElasticDbTenants.TenantManager: Azure Functions app that contains the Durable Functions workflows for creating and deleting tenants
- ElasticDbTenants.App: ASP.NET Core Razor Pages app where tenants can be created and deleted, and todo items can be created and viewed in individual tenants

Note many things are missing from this application that would make it production worthy.
For example, the Razor Pages app has no authentication.

## Running the app

To run this application, you need:

- Azure SQL Database for the catalog DB
  - This can be replaced with any other SQL DB technically but the code will need modifications (Azure AD authentication might not need to be configured)
- Azure SQL Elastic Pool
- Azure SignalR service
  - App can be modified to remove this dependency

Your user account is expected to be an Azure AD admin of the logical Azure SQL server where elastic pool is.
In Azure, the app's Managed Identity would need to be an admin.
The user does not technically need to be an admin for the catalog DB, it only needs read/write access to its tables.

The Functions app and Razor Pages app can be configured through user secrets for local development.

Function app user secrets:

- LocalDevelopmentAadTenantId
  - Your Azure AD tenant id, used for acquiring tokens to create and delete tenant DBs, connect to catalog DB, and connect to tenant DBs
  - Only meant to be used in local development environment
- CatalogDbConnectionString
  - Connection string to the catalog DB, should not contain any username or password since we are using Azure AD authentication
- BackendUsername
  - Username that will get read/write access to the tenant DB after it is created, locally this would be your username, in Azure the Managed Identity name for example
- ElasticPoolSubscriptionId
  - Subscription id where the Elastic Pool is
- ElasticPoolResourceGroup
  - Resource group name where the Elastic Pool is
- ElasticPoolName
  - Name of the Elastic Pool
- ElasticPoolServerName
  - Name of the SQL Server the Elastic Pool is in
- ElasticPoolRegion
  - The Azure region where the Elastic Pool is (e.g. westeurope)

Razor Pages app user secrets:

- LocalDevelopmentAadTenantId
  - Your Azure AD tenant id, used for acquiring tokens to connect to catalog DB and tenant DBs
  - Only meant to be used in local development environment
- CatalogDbConnectionString
  - Connection string to the catalog DB, should not contain any username or password since we are using Azure AD authentication
- AzureSignalrConnectionString
  - Connection string to Azure SignalR service

You will need to run the EF Core migrations for the Catalog DB manually, e.g.:

```sh
# Run in solution folder
dotnet tool restore
dotnet ef database update -s ElasticDbTenants.App -p ElasticDbTenants.CatalogDb -c CatalogDbContext
```
