# CoreCompat1001
This document describes the CoreCompat1001 diagnostic.

## Summary

| Code   | Short Description                                                                        | Type  | Code Fix  | 
| ------ | ---------------------------------------------------------------------------------------- | ----- | --------- | 
| CoreCompat1001 | The reported API is missing in the .Net Core 2.2 runtime. | Error | Unavailable |

## Diagnostic Description

The underlined API is not portable to .Net Core 2.2 runtime because it is not present there. You need to change your code to eliminate its usage.
