// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

// Fails if the Blogger OAuth client_id baked into a build is empty (or, with
// --require-real, still the local-dev placeholder).
//
// The installer is what people Sign In with. CI used to write an empty
// GoogleBloggerv3Secrets.json when GitHub secrets were unset, MSBuild then
// skipped regenerating it, and Google returned "Missing required parameter:
// client_id" (issue #1088). Assert the embedded resource (or a secrets JSON
// file) rather than trusting the workflow plumbing.
//
// Usage:
//   dotnet run scripts/AssertBloggerClientSecrets.cs -- <path-to-dll-or-json> [--require-real]

#nullable disable
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

const string PlaceholderClientId = "PASTE_YOUR_CLIENT_ID_HERE";

var argsList = args.ToList();
bool requireReal = argsList.Remove("--require-real");
if (argsList.Count != 1)
{
    Console.Error.WriteLine("usage: AssertBloggerClientSecrets.cs <path-to-dll-or-json> [--require-real]");
    return 2;
}

var path = ResolvePath(argsList[0]);
if (!File.Exists(path))
{
    Console.Error.WriteLine($"assert-blogger-secrets: '{path}' does not exist.");
    return 1;
}

string json;
if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
{
    json = File.ReadAllText(path);
}
else
{
    json = ReadEmbeddedSecretsJson(path);
    if (json == null)
    {
        Console.Error.WriteLine($"assert-blogger-secrets: '{path}' has no embedded GoogleBloggerv3Secrets.json.");
        return 1;
    }
}

string clientId;
try
{
    using var doc = JsonDocument.Parse(json);
    if (!doc.RootElement.TryGetProperty("installed", out var installed)
        || !installed.TryGetProperty("client_id", out var idElement))
    {
        Console.Error.WriteLine("assert-blogger-secrets: JSON has no installed.client_id.");
        return 1;
    }

    clientId = idElement.GetString() ?? "";
}
catch (JsonException ex)
{
    Console.Error.WriteLine($"assert-blogger-secrets: invalid JSON: {ex.Message}");
    return 1;
}

if (string.IsNullOrWhiteSpace(clientId))
{
    Console.Error.WriteLine(
        "assert-blogger-secrets: client_id is empty. Google will fail Sign In with " +
        "'Missing required parameter: client_id'. Set OLW_BLOGGER_CLIENT_ID / OLW_BLOGGER_CLIENT_SECRET.");
    return 1;
}

if (requireReal && string.Equals(clientId, PlaceholderClientId, StringComparison.Ordinal))
{
    Console.Error.WriteLine(
        "assert-blogger-secrets: client_id is the local-dev placeholder. " +
        "This packaged build would not be able to sign in to Blogger. " +
        "Set the OLW_BLOGGER_CLIENT_ID and OLW_BLOGGER_CLIENT_SECRET GitHub secrets.");
    return 1;
}

Console.WriteLine($"assert-blogger-secrets: client_id is present ({clientId.Length} chars).");
return 0;

static string ResolvePath(string path)
{
    if (Path.IsPathRooted(path) && File.Exists(path))
        return path;

    var cwd = Directory.GetCurrentDirectory();
    var candidates = new[]
    {
        path,
        Path.GetFullPath(path),
        Path.GetFullPath(Path.Combine(cwd, path)),
        // `dotnet run file.cs` sets cwd to the script directory (scripts/).
        Path.GetFullPath(Path.Combine(cwd, "..", path)),
    };
    return candidates.FirstOrDefault(File.Exists) ?? path;
}

static string ReadEmbeddedSecretsJson(string assemblyPath)
{
    using var stream = File.OpenRead(assemblyPath);
    using var pe = new PEReader(stream);
    var reader = pe.GetMetadataReader();
    var resourcesRva = pe.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress;
    if (resourcesRva == 0)
        return null;

    var resourceData = pe.GetSectionData(resourcesRva);
    var blob = resourceData.GetContent();
    foreach (var handle in reader.ManifestResources)
    {
        var resource = reader.GetManifestResource(handle);
        var name = reader.GetString(resource.Name);
        if (!name.EndsWith("GoogleBloggerv3Secrets.json", StringComparison.Ordinal))
            continue;

        // Manifest resource blob: 4-byte length prefix, then UTF-8 JSON.
        int offset = (int)resource.Offset;
        if (offset < 0 || offset + 4 > blob.Length)
            return null;
        int length = BitConverter.ToInt32(blob.AsSpan(offset, 4));
        if (length < 0 || offset + 4 + length > blob.Length)
            return null;
        return Encoding.UTF8.GetString(blob.AsSpan(offset + 4, length));
    }

    return null;
}
