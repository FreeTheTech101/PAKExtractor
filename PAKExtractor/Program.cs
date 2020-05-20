/*
* Project Name:     PAKExtractor
* Project Link:     https://github.com/FreeTheTech101/PAKExtractor
* Purpose:          A simple application for decompressing and converting PAK/XMA files from early Call of Duty titles
* Licence:          GNU General Public License v3.0 or later
* Dependencies:     XMAEncode (Xbox 360 SDK)
*/

// Application dependencies
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

// Ensure that we are using the correct Process class
using Process = System.Diagnostics.Process;

namespace PAKExtractor
{
    class Program
    {
        internal static int Main(string[] args)
        {
            // Create and initalize our accepted command line arguments, along with their descrption
            var rootCommand = new RootCommand
            {
                new Option<string>("--input-pak", description: "Specifies the PAK file to decompress"),
                new Option<string>("--input-directory", description: "Specifies the source directory of one or more XMA file(s)"),
                new Option<string>("--output-directory", getDefaultValue: () => "Output", description: "Specifies the output directory of the decompressed XMA or converted WAV data"),
                new Option<string>("--action", getDefaultValue: () => "Dump", description: "Specifies the function to perform on the provided file or folder"),
            };

            // Set the descrption of our application itself
            rootCommand.Description = "A simple application for decompressing and converting PAK/XMA files from early Call of Duty titles";

            // Ensure that passing any invalid argument invokes the above information to be displayed
            rootCommand.TreatUnmatchedTokensAsErrors = true;

            // Setup our handler, and pass our parsed arguments to it
            rootCommand.Handler = CommandHandler.Create<string, string, string, string>(ParseArguments);
            return rootCommand.InvokeAsync(args).Result;
        }

        /// <summary>
        /// Parses out arguments passed to the application to ensure they are valid, before invoking the correct method with them
        /// </summary>
        internal static void ParseArguments(string inputPAK, string inputDirectory, string outputDirectory, string action)
        {
            // Initalize our bools to determine which action we will be performing
            bool shouldDump = false;
            bool shouldDecompress = false;
            bool shouldConvert = false;

            // Check to see which action is provided
            switch (action.ToLowerInvariant())
            {
                case "dump":
                    shouldDump = true;
                    shouldConvert = true;
                    break;
                case "decompress":
                    shouldDecompress = true;
                    break;
                case "convert":
                    shouldConvert = true;
                    break;
                default:
                    WriteError("Invalid action specified! Expected either dump, decompress, or convert.", 100);
                    break;
            }

            // Check to see if the passed PAK file or XMA source directory exists
            if (shouldDecompress || shouldDump)
            {
                // Ensure that the passed string is not blank, and that the file exists
                // Note: In retail, the file is a .PAK, in the leaked IW4 Alpha, it is .PAKM
                if (string.IsNullOrWhiteSpace(inputPAK) || !File.Exists(inputPAK))
                    WriteError("The specified PAK file does not exist!", 110);
            }
            else
            {
                // Ensure our XMA source directory exists
                if (string.IsNullOrWhiteSpace(inputDirectory) || !Directory.Exists(inputDirectory))
                    WriteError("The specified XMA source directory does not exist!", 120);
            }


            // Create our ouput directory so we know it exists for either path
            if (!Directory.Exists(outputDirectory))
            {
                try
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                catch (Exception e)
                {
                    WriteError($"Failed to create output directory! Error {e.Message}", 210);
                }
            }

            // Finally, begin the requested function
            if (shouldDecompress || shouldDump)
                DecompressPAK(inputPAK, outputDirectory, shouldConvert);
            else if (shouldConvert)
                ConvertXMA(inputDirectory, outputDirectory);
        }

        /// <summary>
        /// Decompresses the specified PAK file <paramref name="inputPAK"/> into the the appropriate folder <paramref name="outputDirectory"/>.
        /// <param name="inputPAK">The input PAK file to decomress.</param>
        /// <param name="outputDirectory">The directory where the XMA files will be outputted to.</param>
        /// </summary>
        internal static void DecompressPAK(string inputPAK, string outputDirectory, bool shouldConvert)
        {
            // Let the user know we are reading the PAK file into memory before doing so
            // Note: This is by no means the best way to do it, but should do
            Console.WriteLine("Reading specified PAK file into memory...");
            byte[] pakFile = File.ReadAllBytes(inputPAK);

            // Let the user know that we are searching for XMA headers before doing so
            // Note: Ensure we do further validation of this found offset, since it is possible to occur in the .XMA data
            Console.WriteLine("Searching for XMA headers...");
            List<int> xmaOffsets = IndexOfSequence(pakFile, new byte[] { 82, 73, 70, 70 });

            // Check to see if we found at least one XMA header in our PAK file
            if (!xmaOffsets.Any())
                WriteError("The specified PAK file contained no valid XMA headers!", 200);
            else
                Console.WriteLine($"Detected {xmaOffsets.Count} possible XMA headers! Decompressing...");


            // Initalize our count to use for file names, then begin to iterate through each offset
            int count = 1;
            try
            {
                // Open up our PAK file (once again) 
                using (FileStream pakStream = new FileStream(inputPAK, FileMode.Open, FileAccess.Read))
                {
                    foreach (int offset in xmaOffsets)
                    {
                        // Jump past the ChunkID so we can read the ChunkSize
                        pakStream.Position = offset + 4;

                        // Read the ChunkSize into memory so we can calculate the full XMA file length
                        byte[] chunkSize = new byte[4];
                        pakStream.Read(chunkSize, 0, 4);

                        // Read the Format into memory so we can validate that this is indeed a valid XMA file
                        byte[] format = new byte[4];
                        pakStream.Read(format, 0, 4);

                        // Convert the bytes into a string, and see if it matches the expected value
                        if (!Encoding.ASCII.GetString(format).Equals("WAVE"))
                        {
                            Console.WriteLine($"Found valid ChunkID with invalid Format at offset {offset}! Skipping...");
                            continue;
                        }

                        // Once we know our header is valid, convert our chunkSize into a uint for later use
                        uint uChunkSize = BitConverter.ToUInt32(chunkSize, 0);

                        // Set the position of our pakStream back to the found offset position so we can dump the data starting from the header
                        pakStream.Position = offset;

                        // Initalize our byte array with the ChunkSize adjusted to account for the ChunkID and ChunkSize field sizes
                        byte[] xmaAudio = new byte[uChunkSize + 8];
                        pakStream.Read(xmaAudio, 0, xmaAudio.Length);

                        // Finally, save our file to the disk and increment our count
                        using (FileStream xmaFile = new FileStream(Path.Combine(outputDirectory, $"{count}.xma"), FileMode.Create, FileAccess.Write))
                            xmaFile.Write(xmaAudio, 0, xmaAudio.Length);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError($"Failed to decompress XMA file! Error {ex.Message}", 210);
            }

            // If requested, begin to convert each XMA into WAV
            if (shouldConvert)
            {
                Console.WriteLine("XMA dumping complete! Converting to WAV...");
                ConvertXMA(outputDirectory, outputDirectory);
            }
            // If not, just let the user know that the decompressing is done
            else
            {
                Console.WriteLine($"Finished decompressing {count - 1} XMA file(s) from {inputPAK}!");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Searches the specified input directory <paramref name="inputDirectory"/> for any .XMA file(s), and converts them into .WAV files into the output directory <paramref name="outputDirectory"/>.
        /// <param name="inputDirectory">The input directory containing .XMA file(s).</param>
        /// <param name="outputDirectory">The output directory to write .WAV file(s) to.</param>
        /// </summary>
        internal static void ConvertXMA(string inputDirectory, string outputDirectory)
        {
            // Check to see if the XMAEncode file exists in the dependencies folder
            if (File.Exists(@"Dependencies\xmaencode.exe"))
            {
                // If the file exists, check to see if the hash matches what we expect
                using (FileStream fileStream = File.Open(@"Dependencies\xmaencode.exe", FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        // Calculate our file hash
                        SHA256 SHA256 = SHA256.Create();
                        string fileHash = Convert.ToBase64String(SHA256.ComputeHash(fileStream));

                        // Check to see if the calculated hash matches the expected hash
                        // Note: This is the hash for XMAEncode 2.0.7645.0
                        if (!fileHash.Equals("dcKq9e7ZoFmPN313S+W9tunV43Mek2WSxydxYXH5hng="))
                        {
                            // If the hash did not match what we expected, prompt the user if they wish to continue
                            Console.WriteLine("XMAEncode hash did not match expected value! Do you wish to continue? [Y/n]");

                            // If the user did not hit the Y key, quit
                            if (Console.ReadKey().Key != ConsoleKey.Y)
                                Environment.Exit(1);
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError($"Failed to validate XMAEncode hash! Error {ex.Message}", 300);
                    }
                }
            }
            else
                WriteError("Failed to find XMAEncode in Dependencies folder!", 310);

            // Find every file in our inputDirectory that has the correct file extension
            // Note: Thankfully it seems that IW and T did not rename these files in the alphas, so we are good with this check
            IEnumerable<string> xmaList = Directory.GetFiles(inputDirectory, "*.XMA", SearchOption.AllDirectories);

            // Check to see if we found at least one XMA header in our PAK file
            if (!xmaList.Any())
                WriteError("The specified directory contains no XMA files!", 200);
            else
                try
                {
                    foreach (string file in xmaList)
                    {
                        // Get the fully specified input file name and output file location
                        string inputFile = Path.Combine(Path.GetFullPath(file));
                        string outputFile = Path.Combine(Path.GetFullPath(outputDirectory), Path.GetFileNameWithoutExtension(file) + ".wav");

                        // Create our XMAEncode process info, then start it
                        using (Process convertProcess = new Process())
                        {
                            convertProcess.StartInfo.FileName = @"Dependencies\xmaencode.exe";
                            convertProcess.StartInfo.Arguments = $"{inputFile} /X {outputFile}";
                            convertProcess.StartInfo.WorkingDirectory = "";
                            convertProcess.StartInfo.UseShellExecute = false;
                            convertProcess.StartInfo.RedirectStandardOutput = true;
                            convertProcess.StartInfo.RedirectStandardError = true;
                            convertProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            convertProcess.StartInfo.CreateNoWindow = true;
                            convertProcess.Start();
                            convertProcess.WaitForExit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteError($"Failed to decompress XMA file into WAV! Error {ex.Message}", 300);
                }

            // Let the user know we are done convering 
            Console.WriteLine($"Finished converting all XMA files in {inputDirectory}!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

        /// <summary>
        /// Searches the specified byte array <paramref name="fileBuffer"/> for a byte pattern <paramref name="searchPattern"/>, and returns all found offsets.
        /// <param name="fileBuffer">The byte array to search.</param>
        /// <param name="searchPattern">The pattern to search for.</param>
        /// </summary>
        internal static List<int> IndexOfSequence(byte[] fileBuffer, byte[] searchPattern)
        {
            // Create our list of int offsets
            List<int> offsets = new List<int>();

            // Find the first instance of our search pattern
            int i = Array.IndexOf(fileBuffer, searchPattern[0], 0);

            // Loop through our fileBuffer, and find any instances of our pattern
            while (i >= 0 && i <= fileBuffer.Length - searchPattern.Length)
            {
                // Create our byte buffer, and copy the next set of bytes equal to the length of our search pattern into it
                byte[] segment = new byte[searchPattern.Length];
                Buffer.BlockCopy(fileBuffer, i, segment, 0, searchPattern.Length);

                // If the contents of the segment are equal to our search pattern, add it's offset to our list
                if (segment.SequenceEqual(searchPattern))
                    offsets.Add(i);

                // Update our offset for our next search
                i = Array.IndexOf(fileBuffer, searchPattern[0], i + 1);
            }

            // Return our list of all found offsets
            return offsets;
        }

        /// <summary>
        /// Prints the specified error string <paramref name="error"/> to the console in red text, before closing the the program with <paramref name="exitCode"/> exit code.
        /// <param name="error">The error to write to the console.</param>
        /// <param name="exitCode">The exit code the program closes with.</param>
        /// </summary>
        internal static void WriteError(string error, int exitCode)
        {
            // Read the users current console font colour before changing it
            ConsoleColor defaultColour = Console.ForegroundColor;

            // Set the font colour to dark red, then print the error
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(error);
            Console.WriteLine("Press any key to continue...");

            // Reset the users font colour, then kill the program after they press any key
            Console.ForegroundColor = defaultColour;
            Environment.Exit(exitCode);
        }
    }
}