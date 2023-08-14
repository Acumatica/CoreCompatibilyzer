# CoreCompatibilyzer

CoreCompatibilyzer is a static code analyzer that checks .Net code for compatibility with .Net Core 2.2 runtime

Welcome to CoreCompatibilyzer, the static .Net code analyzer that checks .Net Framework code for compatibility with .Net Core 2.2. There are two options to run the analysis - either install CoreCompatibilyzer VSIX extension or use the console runner.

## CoreCompatibilyzer console runner

The console runner provides several command line arguments that configure the static code analysis and the format of the report generated by the console runner. You can run `--help` to see their description in the console window. 
Below is the list of command line arguments:

| Argument  |  Description                                                                        |
| ------    | ----------------------------------------------------------------------------------- | 
|  codeSource (position 0)             | Required. A path to the "code source" which will be validated. The term "code source" is a generalization for components/services that can provide source code to the tool. Currenly, the supported code sources are C# projects and C# solutions. | 
| -v, &#8209;&#8209;verbosity          | This optional parameter allows you to explicitly specify logger verbosity. The allowed values are taken from the "Serilog.Events.LogEventLevel enum. The allowed values: `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`. By default, the logger will use the `Information` verbosity. | 
| &#8209;&#8209;noSuppression          | When this optional flag is set to true, the code analysis would not take into consideration suppression comments present in the code and will report suppressed diagnostics. | 
| &#8209;&#8209;msBuildPath            | This optional parameter allows you to provide explicitly a path to the MSBuild tool that  will be used for analysis. By default, MSBuild installations will be searched automatically on the current machine and the latest found version will be used. | 
| &#8209;&#8209;withUsages             | By default, the report output includes only a shortened list of used banned API. Set this flag to include the locations of used banned API calls into the report. | 
| &#8209;&#8209;showMembersOfUsedType  | When report is displayed in a shortened form without banned API calls locations, it could be shortened even more. By default, the report will not display used banned type member APIs if their containing type is also banned and used by the code being analyzed. Set this flag to include the banned type member APIs into the report together with their containing type. This flag does not affect the report when the IncludeApiUsages is set. | 
| -g, &#8209;&#8209;grouping           | This parameter allows you to specify the grouping of API calls. By default there is no grouping. You can make the grouping of the reported API calls by namespaces, types, APIs or any combination of them: |
|                                      |      - Add `n` or `N` to group API usages by namespaces,  |
|                                      |      - Add `t` or `T` to group API usages by types, |
|                                      |      - Add `a` or `A` to group API usages by APIs. | 
| -f, &#8209;&#8209;file               | The name of the output file. If not specified then the report will be outputted to the console window. | 
| &#8209;&#8209;outputAbsolutePaths    | When report is set to output the detailed list of banned APIs with their usages this flag regulates how the locations of API usages will be ouput. By default, file paths in locations are relative to the containing project directory. However, if this flag is set then the absolute file paths will be used. This flag does not affect the report when the `IncludeApiUsages` is not set. | 
| &#8209;&#8209;format                 | The report output format. There are two supported values: |
|                                      | - `text` to ouput the report in plain text, this is the default output mode,  |
|                                      | - `json` to output the report in JSON format.  |
| &#8209;&#8209;help                   | Display this help screen. |
| &#8209;&#8209;version                | Display version information. |