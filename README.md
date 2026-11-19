# Ceryle.Client

.NET client for the Ceryle document rendering service.

## Registration

```csharp
services.AddCeryleClient(options =>
{
    options.BaseUrl = "https://ceryle.example.com";
    options.ApiKey = "your-api-key";
});
```

`ICeryleClient` supports health checks, template listing/upload/deletion, synchronous rendering, asynchronous task creation, and task result downloads.

## Publishing

Push a semantic version tag with a `v` prefix to publish the package to the repository owner's private GitHub Packages NuGet feed:

```bash
git tag v1.0.0
git push origin v1.0.0
```
