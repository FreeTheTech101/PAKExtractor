# PAKExtractor

## Introduction

A simple application for decompressing and converting PAK/XMA files from early Call of Duty titles

## Dependencies

[.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) [or above](https://dotnet.microsoft.com/download/dotnet-core)

[System.CommandLine](https://github.com/dotnet/command-line-api) [NuGet package](https://nuget.org/packages/System.CommandLine)

XMAEncode from the Xbox 360/One SDK (Required for XMA conversion)

## Compiling

Re-add the XMAEncode binary to the `Dependencies` folder, and restore any required NuGet packages

## Usage

Run the PAKExtractor application from the output directory, providing a mix of the following arguments

## Arguments

| Argument           | Function                                                                                                               | Default  |
|--------------------|------------------------------------------------------------------------------------------------------------------------|----------|
|  --input-pak       | Specifies the PAK file to decompress                                                                                   | N/A      |
|  --input-directory | Specifies the source directory of one or more XMA file(s)                                                              | N/A      |
| --output-directory | Specifies the output directory of the decompressed XMA or decoded WAV data                                             | .\Output |
| --function         | Specifies the function to perform on the provided file or folder | Dump     |

## Functions

| Function   | Action                                                                                                       |
|------------|--------------------------------------------------------------------------------------------------------------|
| Dump       | Takes the specified PAK file, decompress all found XMA files to the disk, then converts all XMA files to WAV |
| Decompress | Takes the specified PAK file, and decompress all found XMA files to the disk                                 |
| Convert    | Takes the specified input directory, and recursivly converts all XMA files to WAV                            |
