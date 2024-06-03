using System.Diagnostics;

using Volatility.TextureHeader;

namespace Volatility.Utilities;

internal class PS3TextureUtilities
{
    public static void PS3GTFToDDS(TextureHeaderPS3 ps3Header, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        byte[] header = new byte[0xE];
        using MemoryStream ps3Stream = new(header);
        using BinaryWriter writer = new(ps3Stream);
                
        ps3Header.WriteToStream(writer);
        ps3Stream.ReadExactly(header, 0, 0xE);

        writer.Close();
        ps3Stream.Close();

        PS3GTFToDDS(header, sourceBitmapPath, destinationBitmapPath, verbose); 
    }

    public static void PS3GTFToDDS(string ps3HeaderPath, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        using FileStream ps3Stream = new(ps3HeaderPath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new(ps3Stream);

        byte[] ps3Header = reader.ReadBytes(0xE);

        reader.Close();
        ps3Stream.Close();

        PS3GTFToDDS(ps3Header, sourceBitmapPath, destinationBitmapPath, verbose);
    }

    public static void PS3GTFToDDS(byte[] ps3Header, string sourceBitmapPath, string destinationBitmapPath, bool verbose = false)
    {
        Array.ConstrainedCopy(ps3Header, 0, ps3Header, 0, 0xE);

        byte[] fileBytes = File.ReadAllBytes(sourceBitmapPath);
        byte[] size = BitConverter.GetBytes(fileBytes.Length);
        Array.Reverse(size);

        byte[] gtf = new byte[]
        {
            0x02, 0x02, 0x00, 0xFF,
        }
        .Concat(size)
        .Concat(new byte[]
        {
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x80,
        })
        .Concat(size)
        .Concat(ps3Header)
        .Concat(new byte[0x5A])
        .Concat(fileBytes)
        .ToArray();

        File.WriteAllBytes($"{destinationBitmapPath}.gtf", gtf);

        string gtf2ddsPath = $"tools{Path.DirectorySeparatorChar}gtf2dds.exe";

        if (!File.Exists(gtf2ddsPath))
        {
            throw new FileNotFoundException("Unable to find external tool gtf2dds.exe!");
        }

        ProcessStartInfo start = new ProcessStartInfo
        {
            FileName = gtf2ddsPath,
            Arguments = $"-o \"{destinationBitmapPath}.dds\" \"{destinationBitmapPath}.gtf\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (verbose) Console.WriteLine($"Running: tools{Path.DirectorySeparatorChar}gtf2dds.exe -o \"{destinationBitmapPath}.dds\" \"{destinationBitmapPath}.gtf\"");

        using (Process process = new Process())
        {
            if (verbose) Console.WriteLine("Converting PS3 GTF texture to DDS...");

            process.StartInfo = start;
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }

        fileBytes = File.ReadAllBytes($"{destinationBitmapPath}.dds");

        if (fileBytes.Length > 0x80)
        {
            byte[] newBytes = new byte[fileBytes.Length - 0x80];
            Array.Copy(fileBytes, 0x80, newBytes, 0, newBytes.Length);

            try
            {
                File.WriteAllBytes(destinationBitmapPath, newBytes);
            }
            catch (IOException e)
            {
                Console.WriteLine($"Error trying to write trimmed DDS data for {Path.GetFileNameWithoutExtension(sourceBitmapPath)}: {e.Message}");
            }

            if (verbose) Console.WriteLine("Trimmed converted DDS header.");
        }
        else
        {
            Console.WriteLine($"Error trying to write trimmed DDS data for {Path.GetFileNameWithoutExtension(sourceBitmapPath)}: Texture file is too short! Not a DDS file.");
        }
    }
}
