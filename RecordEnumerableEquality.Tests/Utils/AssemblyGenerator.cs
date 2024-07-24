using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordEnumerableEquality.Tests.Utils;

internal static class AssemblyGenerator
{
    public static TemporaryAssembly Generate(string code)
    {
        // Create a syntax tree from the source code
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);

        // Define references to necessary assemblies
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        };

        // Create a compilation object
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Emit the compilation to a DLL
        string outputPath = "TestAssembly.dll";

        var ta = TemporaryAssembly.CreateEmpty();

        using var ms = ta.OpenWrite();
        EmitResult result = compilation.Emit(ms);
        if (!result.Success)
        {
            ta.Dispose();
            // Display compilation errors
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            throw new Exception("Compilation failed");
        }

        return ta;
    }
}

public class TemporaryAssembly : IDisposable
{
    private readonly TempFile _tempFile;

    public string Location => _tempFile.Location;

    public TemporaryAssembly(string tempPath)
        : this(new TempFile(tempPath))
    {
    }

    public TemporaryAssembly(TempFile tempFile)
    {
        _tempFile = tempFile;
    }

    public Stream OpenWrite()
    {
        return _tempFile.OpenWrite();
    }

    public static TemporaryAssembly CreateEmpty()
    {
        return new TemporaryAssembly(TempFile.CreateWithExtension(".dll"));
    }

    public static TemporaryAssembly FromStream(Stream stream)
    {
        var tempFile = TempFile.CreateWithExtension(".dll");
        tempFile.OverwriteFrom(stream);

        return new TemporaryAssembly(tempFile);
    }

    public void Dispose()
    {
        ((IDisposable)_tempFile).Dispose();
    }
}

public class TempFile : IDisposable
{
    public string Location { get; }

    public bool Exists => System.IO.File.Exists(Location);

    public TempFile(string path)
    {
        Location = path;
    }

    /// <summary>
    /// Creates a TempFile with a random name in the temp directory.
    /// </summary>
    /// <returns></returns>
    public static TempFile CreateRandom()
    {
        return CreateWithExtension(".tmp");
    }

    /// <summary>
    /// Finds a random filename that does not exist in the temp directory and creates a TempFile with that name.
    /// Does not actually create the file on disk.
    /// </summary>
    /// <param name="extension"></param>
    /// <returns></returns>
    public static TempFile CreateWithExtension(string extension)
    {
        var tempDirectory = System.IO.Path.GetTempPath();
        while (true)
        {
            var randomName = Random.Shared.NextString(10);
            var sb = new StringBuilder();
            sb.Append(tempDirectory);
            sb.Append(randomName);

            if (extension.StartsWith("."))
            {
                sb.Append(extension);
            }
            else
            {
                sb.Append('.');
                sb.Append(extension);
            }

            var newPath = sb.ToString();
            if (!System.IO.File.Exists(newPath))
            {
                return new TempFile(newPath);
            }
        }
    }

    public async Task WriteAllTextAsync(string content)
    {
        using var stream = System.IO.File.OpenWrite(Location);
        using var writer = new StreamWriter(stream);
        await writer.WriteAsync(content);
    }

    // ReadAllTextAsync
    public async Task<string> ReadAllTextAsync()
    {
        using var stream = System.IO.File.OpenRead(Location);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task WriteAllBytesAsync(byte[] bytes)
    {
        await System.IO.File.WriteAllBytesAsync(Location, bytes);
    }

    public void OverwriteFrom(FileInfo sourcePath)
    {
        System.IO.File.Copy(sourcePath.FullName, Location, true);
    }

    public void OverwriteFrom(string sourcePath)
    {
        System.IO.File.Copy(sourcePath, Location, true);
    }

    public void OverwriteFrom(Stream stream)
    {
        using var fs = new FileStream(Location, FileMode.Create, FileAccess.Write);
        fs.Position = 0;
        stream.CopyTo(fs);
        if (fs.Position < fs.Length)
        {
            fs.SetLength(fs.Position);
        }
    }

    public Stream OpenWrite()
    {
        FileEx.DeleteIfExists(Location);
        return System.IO.File.OpenWrite(Location);
    }

    public void Dispose()
    {
        FileEx.DeleteIfExists(Location);
    }
}

public static class FileEx
{
    public static void DeleteIfExists(string filename)
    {
        if (System.IO.File.Exists(filename))
        {
            System.IO.File.Delete(filename);
        }
    }

    public static FileStream OpenReadAsync(string filename)
    {
        return new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
    }
}

public static class RandomExtensions
{
    private const string DefaultCharset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    // NextMilliseconds
    public static TimeSpan NextMilliseconds(this Random random, int min, int max)
    {
        return TimeSpan.FromMilliseconds(random.Next(min, max));
    }

    public static TimeSpan NextTimeSpan(this Random random, TimeSpan min, TimeSpan max)
    {
        return random.NextMilliseconds((int)min.TotalMilliseconds, (int)max.TotalMilliseconds);
    }

    public static string NextString(this Random random, int length, string charset = DefaultCharset)
    {
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            sb.Append(charset[random.Next(charset.Length)]);
        }
        return sb.ToString();
    }
}