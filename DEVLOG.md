# Dev Log

## 2020-04-27

Created the repo with:

```bash
dotnet new grpc -o dotnet-grpc-poc
cd dotnet-grpc-poc
code -r .
```

Vs code did some stuff:

```
Installing C# dependencies...
Platform: darwin, x86_64

Downloading package 'OmniSharp for OSX' (47441 KB).................... Done!
Validating download...
Integrity Check succeeded.
Installing package 'OmniSharp for OSX'

Downloading package '.NET Core Debugger (macOS / x64)' (41849 KB).................... Done!
Validating download...
Integrity Check succeeded.
Installing package '.NET Core Debugger (macOS / x64)'

Downloading package 'Razor Language Server (macOS / x64)' (51065 KB).................... Done!
Installing package 'Razor Language Server (macOS / x64)'

Finished
```

It also asked for help on something that disappeared rather quickly...

Installed `vscode-proto3` to get some protobuf highlighting

Apparently, you have to turn off TLS for MacOS grpc dev?

```
// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
```

After following the instructions and creating a non-TLS endpoint, `dotnet run` works.
The instructions were to allow a TLS override for localhost.

---

Added a tool manifest and dotnet format:

```bash
dotnet new tool-manifest
dotnet tool install dotnet-format
```

Added a `Makefile` to make running some scripts easier.  Can run format from it:

```bash
make fmt
```

---

Apparently I didn't setup my project with the correct format? In order to have tests I should
have a project folder for my service ***AND*** my tests.

How I fixed:

* Recreate the project in a src folder:
  ```bash
  dotnet new grpc -o src/DotnetGrpcPoc
  ```
  Applied fixes from above
* Modify `Makefile` `fmt` script:
  ```bash
  dotnet tool run dotnet-format -f .
  ```
  Formats the entire directory tree
* Add new `run` convenience script
  ```
  make run
  dotnet run --project src/DotnetGrpcPoc/
  ```

