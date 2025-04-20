# CoreCompatibilyzer Release Notes
This document provides information about fixes, enhancements, and key features that are available in CoreCompatibilyzer.

## CoreComparibilyzer 1.1.0 (Current): January 31, 2025
CoreCompatibilyzer 1.1.0 provides the new features and bugfixes described in this section, as well as the features described below for previous versions of CoreCompatibilyzer.

### New Features
CoreComparibilyzer 1.1.0 introduces the following new features: 
- A way of grouping reported APIs by source files that contain the reported calls to incorrect APIs.
- Support of ARM64 architecture in a Visual Studio extension.

### Code Fixes
CoreComparibilyzer 1.1.0 contains the following code fixes:
- Incorrectly banned APIs have been removed from the list of banned APIs incompatible with .Net Core 2.2.
- The analysis of extension methods that were not recognized as banned APIs has been fixed.
- Support for the analysis of conditional access expressions (like `obj?.Member`) has been added.
- The analysis of the member access expression has been fixed; CoreCompatibilyzer now analyzes the type of the accessed expression.
- The analysis of array type symbols has been fixed.
- The `.editorconfig` file has been added to the CoreCompatibilyzer project to enforce a consistent code style.
- CoreCompatibilyzer code has been refactored to improve performance and readability.


## CoreComparibilyzer 1.0.0: September 1, 2023
CoreComparibilyzer 1.0.0 is the initial release. It introduces the following features.

### Diagnostics
CoreComparibilyzer finds uses of .Net Framework APIs that are incompatible with .Net Core 2.2 and reports them with one of the diagnostics described below.

| Code   | Short Description                                       | Type  | Code Fix  |
| ------ | ------------------------------------------------------- | ----- | --------- |
| [CoreCompat1001](diagnostics/CoreCompat1001.md) | The reported API is missing in the .Net Core 2.2 runtime. | Error | Unavailable |
| [CoreCompat1002](diagnostics/CoreCompat1002.md) | The underlined API is not portable to .Net Core 2.2 runtime because the API is obsolete. | Error | Unavailable |

### Visual Studio Extension
CoreComparibilyzer provides an extension for Visual Studio to see analyzer alerts directly in the code editor. 

### Console Runner 
CoreComparibilyzer provides a console runner to run the analysis from the command line for the entire solution or project manually or in CI scripts. The console runner also provides advanced options for the report formatting style and output format.
You can use a variety of command-line arguments to configure the reporting and output. You can run `--help` to see the available options and their descriptions in the console window.
