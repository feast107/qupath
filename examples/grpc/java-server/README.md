Java example gRPC server (minimal)

Purpose
-------
This example shows a minimal Java gRPC server implementing `ImageService` from `protos/qupath.proto`.

Generate Java sources
---------------------
From the repository root run (requires Gradle):

```bash
cd examples/grpc/java-server
./gradlew build
```

Gradle will use the `protobuf` plugin to generate Java classes from `protos/qupath.proto`.

Run server
----------
After build, run:

```bash
./gradlew run
```

The server listens on port 50051 and provides a trivial `GetImageMeta` and `StreamTile` implementation for testing.
