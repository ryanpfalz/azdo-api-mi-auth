using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.WebApi;

namespace Company.Function
{
    public static class TestMIHttpTrigger
    {
        public const string AdoBaseUrl = "https://dev.azure.com";

        public const string AdoOrgName = "<your-org-name>";

        public const string AadTenantId = "<your-tenant-guid>";
        // ClientId for User Assigned Managed Identity. Leave null for System Assigned Managed Identity
        public const string AadUserAssignedManagedIdentityClientId = "<your-user-assigned-mi-client-guid>";

        // Credentials object is static so it can be reused across multiple requests. This ensures
        // the internal token cache is used which reduces the number of outgoing calls to Azure AD to get tokens.
        // 
        // DefaultAzureCredential will use VisualStudioCredentials or other appropriate credentials for local development
        // but will use ManagedIdentityCredential when deployed to an Azure Host with Managed Identity enabled.
        // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme?view=azure-dotnet#defaultazurecredential
        private readonly static TokenCredential credential =
            new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    TenantId = AadTenantId,
                    ManagedIdentityClientId = AadUserAssignedManagedIdentityClientId,
                    ExcludeEnvironmentCredential = true // Excluding because EnvironmentCredential was not using correct identity when running in Visual Studio
                });

        public static List<ProductInfoHeaderValue> AppUserAgent { get; } = new()
        {
            new ProductInfoHeaderValue("Identity.ManagedIdentitySamples", "1.0"),
            new ProductInfoHeaderValue("(3-AzureFunction-ManagedIdentity)")
        };

        [FunctionName("TestMIHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (!int.TryParse(req.Query["pipelineId"], out int pipelineId))
            {
                return new BadRequestObjectResult($"Invalid Pipeline ID: {req.Query["pipelineId"]}.");
            }

            string projectName = req.Query["projectName"];
            if (string.IsNullOrEmpty(projectName))
            {
                return new BadRequestObjectResult($"Invalid Project Name: {req.Query["projectName"]}.");
            }

            //hardcoded
            //var pipelineId = 1;
            //var projectName = "Sandbox";

            var vssConnection = await CreateVssConnection();

            try
            {
                var pipelineClient = new Microsoft.Azure.Pipelines.WebApi.PipelinesHttpClient(vssConnection.Uri, vssConnection.Credentials);

                var response = await pipelineClient.RunPipelineAsync(new RunPipelineParameters(), projectName, pipelineId);

                return response != null
                    ? (ActionResult)new OkObjectResult($"Pipeline run started: {response.Id}")
                    : new BadRequestObjectResult("Pipeline run failed to start");
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex.Message);
            }

        }

        private static async Task<VssConnection> CreateVssConnection()
        {
            var accessToken = await GetManagedIdentityAccessToken();
            var token = new VssAadToken("Bearer", accessToken);
            var credentials = new VssAadCredential(token);

            var settings = VssClientHttpRequestSettings.Default.Clone();
            settings.UserAgent = AppUserAgent;

            var organizationUrl = new Uri(new Uri(AdoBaseUrl), AdoOrgName);
            return new VssConnection(organizationUrl, credentials, settings);
        }

        private static async Task<string> GetManagedIdentityAccessToken()
        {
            var tokenRequestContext = new TokenRequestContext(VssAadSettings.DefaultScopes);
            var token = await credential.GetTokenAsync(tokenRequestContext, CancellationToken.None);

            return token.Token;
        }

    }
}