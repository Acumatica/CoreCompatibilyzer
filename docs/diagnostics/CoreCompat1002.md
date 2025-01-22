# CoreCompat1002
This document describes the CoreCompat1002 diagnostic.

## Summary

| Code   | Short Description                                                                        | Type  | Code Fix  | 
| ------ | ---------------------------------------------------------------------------------------- | ----- | --------- | 
| CoreCompat1002 | The underlined API is not portable to .Net Core 2.2 runtime because the API is obsolete. | Error | Unavailable |

## Diagnostic Description

The underlined API is not portable to .Net Core 2.2 runtime because the API is obsolete. A call to this API will throw `PlatformNotSupportedException`. You need to change your code to eliminate its usage.
