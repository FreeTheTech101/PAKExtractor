# PAKExtractor

## Introduction

A simple application for decompressing and converting PAK/XMA files from early Call of Duty titles.

## Compiling

Re-add the XMAEncode application from the Xbox 360 SDK, then compile using at least Visual Studio 2019 with .NET Core 3.1

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