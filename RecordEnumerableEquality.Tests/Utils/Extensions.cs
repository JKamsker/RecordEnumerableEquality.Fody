using Fody;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordEnumerableEquality.Tests.Utils;

internal static class Extensions
{
    public static void OpenInDnSpy(this TestResult testResult)
    {
        var dnspy = _dnSpyPath.Value;
        if (string.IsNullOrWhiteSpace(dnspy))
        {
            return;
        }

        var dll = testResult.AssemblyPath;
        System.Diagnostics.Process.Start(dnspy, dll);
    }
    
    private static Lazy<string> _dnSpyPath = new Lazy<string>(GetDnSpyPath);
    
    // 1. Envkey: dnspy64, return if exists
    // 2. Check if C:\Users\W31rd0\tools\dnSpy\x64\dnSpy.exe exists
    private static string GetDnSpyPath()
    {
        var envkey = "dnspy64";
        var dnspy = Environment.GetEnvironmentVariable(envkey);
        if (!string.IsNullOrWhiteSpace(dnspy))
        {
            return dnspy;
        }

        // Find in path
        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(';');
        if (paths != null)
        {
            var found = paths
                .Select(p => System.IO.Path.Combine(p, "dnSpy.exe"))
                .Concat(paths.Select(p => System.IO.Path.Combine(p, "dnSpy.lnk")))
                .Concat(paths.Select(p => System.IO.Path.Combine(p, "dnSpy64.exe")))
                .FirstOrDefault(System.IO.File.Exists);

            if (!string.IsNullOrWhiteSpace(found))
            {
                return found;
            }
        }

        var defaultPath = @"C:\Users\W31rd0\tools\dnSpy\x64\dnSpy.exe";
        if (System.IO.File.Exists(defaultPath))
        {
            return defaultPath;
        }

        return String.Empty;
    }
}