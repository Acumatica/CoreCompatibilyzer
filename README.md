# CoreCompatibilyzer

Welcome to CoreCompatibilyzer, a static .Net code analyzer that checks .Net Framework code for compatibility with .Net Core 2.2. There are two options to run the analysis - either install CoreCompatibilyzer VSIX extension or use the console runner.
You can find Release Notes [here](./docs/ReleaseNotes.md).

## Analysis

The analysis itself is not very complex. All API usages are checked against a list of the APIs incompatible with .Net Core 2.2. If API, its containing types or containing namespace are in the list of incompatible APIs, then it is marked as incompatible. 
There is a list of banned APIs that is stored  in the tool's `.\ApiData\Data` subfolder in the `BannedApis.txt` file. There is also a file `WhiteList.txt` with the whitelisted APIs which are not reported by the analyzer even if they are recognized as incompatible. 
Sometimes code may contain types declared in system namespaces such as `System.Web.Compilation.CustomBuildManager` and we don't want to report such types. The open list of APIs loaded by application at runtime provides an easy way to configure the analysis. 

You can find list of diagnostics in this [summary](./docs/Summary.md).

You can forbid the usage of some API by adding it to the list of banned APIs or white list an existing banned API by adding it to the list of allowed APIs. The format of API records is described in the next section:

### API format

The APIs records in both files have the same API format which is based on the `DocID` format: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings. 
This format is chosen due to its convenience - it is widely used by .Net technology and Roslyn already has a built-in functionality to generate `DocID` for APIs. However, the Doc ID format was slightly modified to store more information about the API:
- In the full API name namespace is separated from the containing type names with a special hyphen `-` separator. This allows us to parse API name parts and calculated which part is the namespace name which is impossible to do in all cases for the standard `DocID` format. Here is an example:
  ```cs 
  T:System.Data-UpdateException
  ```
- If the API name includes nested types, then their names are separated by `+` to simplify the parsing of the API name into parts:
  ```cs
  T:Microsoft.AspNet.Mvc.ModelBinding-ModelStateDictionary+PrefixEnumerable
  ```
- There are two kinds of incompatible APIs - API missing in the .Net Core 2.2 and API obsolete in the .Net Core 2.2. The latter one won't cause compilation errors after migration to .Net Core 2.2 but throw exceptions when they are called at runtime. 
Such APIs are marked with a special obsolete marker character `O` at the end:
 ```cs
 M:System-Uri.EscapeUriString(System.String) O
 ```

## CoreCompatibilyzer console runner

The console runner provides several command line arguments that configure the static code analysis and the format of the report generated by the console runner. You can run `--help` to see their description in the console window. 
Below is the list of command line arguments:

| Argument  |  Description                                                                        |
| ------    | ----------------------------------------------------------------------------------- | 
|  codeSource (position 0)             | Required. A path to the "code source" which will be validated. The term "code source" is a generalization for components/services that can provide source code to the tool. Currently, the supported code sources are C# projects and C# solutions. | 
| -v, &#8209;&#8209;verbosity          | This optional parameter allows you to explicitly specify logger verbosity. The allowed values are taken from the "Serilog.Events.LogEventLevel enum. The allowed values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`. By default, the logger will use the `Information` verbosity. | 
| &#8209;&#8209;noSuppression          | When this optional flag is specified, the code analysis would not take into consideration suppression comments present in the code and will report suppressed diagnostics. | 
| &#8209;&#8209;msBuildPath            | This optional parameter allows you to provide explicitly a path to the MSBuild tool that  will be used for analysis. By default, MSBuild installations will be searched automatically on the current machine and the latest found version will be used. | 
| &#8209;&#8209;withUsages             | By default, the report output includes only a shortened list of used banned API. Set this flag to include the locations of used banned API calls into the report. | 
| &#8209;&#8209;withDistinctApis       | If this flag is specified then the report will start with a list of all distinct APIs used by the code source. | 
| &#8209;&#8209;showMembersOfUsedType  | When report is displayed in a shortened form without banned API calls locations, it could be shortened even more. By default, the report will not display used banned type member APIs if their containing type is also banned and used by the code being analyzed. Set this flag to include the banned type member APIs into the report together with their containing type. This flag does not affect the report when the `--withUsages` is specified. | 
| -g, &#8209;&#8209;grouping           | This parameter allows you to specify the grouping of API calls. By default there is no grouping. You can make the grouping of the reported API calls by namespaces, types, APIs or any combination of them: |
|                                      |      - Add `f` or `F` to group API usages by source files,  |
|                                      |      - Add `n` or `N` to group API usages by namespaces,  |
|                                      |      - Add `t` or `T` to group API usages by types, |
|                                      |      - Add `a` or `A` to group API usages by APIs. | 
| -f, &#8209;&#8209;file               | The name of the output file. If not specified then the report will be outputted to the console window. | 
| &#8209;&#8209;outputAbsolutePaths    | When report is set to output the detailed list of banned APIs with their usages this flag regulates how the locations of API usages will be output. By default, file paths in locations are relative to the containing project directory. However, if this flag is set then the absolute file paths will be used. This flag does not affect the report when the `--withUsages` is not specified. | 
| &#8209;&#8209;format                 | The report output format. There are two supported values: |
|                                      | - `text` to output the report in plain text, this is the default output mode,  |
|                                      | - `json` to output the report in JSON format.  |
| &#8209;&#8209;help                   | Display this help screen. |
| &#8209;&#8209;version                | Display version information. |