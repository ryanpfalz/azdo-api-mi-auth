1. Referring to https://github.com/microsoft/azure-devops-auth-samples/tree/master/ServicePrincipalsSamples/ClientLibsNET/3-AzureFunction-ManagedIdentity

2. Create a new Function project in VS

3. Add the code from AzDOFunction/Function1.cs

4. Using dotnet CLI, run:
   dotnet add package Microsoft.VisualStudio.Services.InteractiveClient --version 19.232.0-preview
   dotnet add package Microsoft.TeamFoundationServer.Client --version 19.232.0-preview
   dotnet add package Azure.Identity --version 1.10.4
   dotnet add package Microsoft.NET.Sdk.Functions --version 4.2.0

5. Ensure you're signed into VS with an account that has access to your Azure Tenant

6. Update the strings to match your Azure Tenant and AzDO Org

