# Build Instructions

The program was built using Visual Studio 2017. After opening the solution, make sure you have **CefSharp.WinForms** and **Microsoft.VC120.CRT.JetBrains** included - if not, download them using NuGet.
```
PM> Install-Package CefSharp.WinForms -Version 57.0.0
PM> Install-Package Microsoft.VC120.CRT.JetBrains
```

When building the `Release` configuration, the folder will only contain files intended for distribution (no debug symbols or other unnecessary files). You may package the files yourself, or use `bld/RUN BUILD.bat` to generate installer files using Inno Setup (make sure the Inno Setup binaries are on your PATH).

Built files are available in **bin/x86**, installer files are generated in **bld/Output**. If you decide to release a custom version publicly, please make it clear that it is not the original TweetDuck. 
