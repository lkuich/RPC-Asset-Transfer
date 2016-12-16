#RPC Asset Transfer
A simple quick and dirty C# gRPC example application for transferring assets via Bi-directional RPC streams<br>

##Setup Server
- Open `AssetTransferServer\Program.cs`
- Add your bundles to the `bundles` variable. Point to an existing directory containing assets and give it a unique ID (just an int will do for this example project)
- Change the `PORT` if that one is already in use

##Setup Client
- Open `AssetTransferClient\Program.cs`
- Change `WORKING_DIR` to the dir you want the client to write to
- Change the `PORT` if that one is already in use (make sure it matches the `PORT` defined in the server)

##Running
- Start the Server.
- Start the Client. Enter the target bundle ID and press return. Cached assets will be streamed to the working directory

##TODO:
- Determine when a stream has finished and validate all files were actually received