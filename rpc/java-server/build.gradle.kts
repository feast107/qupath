plugins {
  `java`
  id("com.google.protobuf") version "0.8.19"
}

group = "io.github.qupath"
version = "0.1.0"

repositories { mavenCentral() }

val grpcVersion = "1.56.0"
val protobufVersion = "3.24.3"

dependencies {
  implementation("io.grpc:grpc-netty-shaded:$grpcVersion")
  implementation("io.grpc:grpc-protobuf:$grpcVersion")
  implementation("io.grpc:grpc-stub:$grpcVersion")
  implementation("com.google.protobuf:protobuf-java:$protobufVersion")
}

java {
  sourceCompatibility = JavaVersion.VERSION_17
  targetCompatibility = JavaVersion.VERSION_17
}

protobuf {
  protoc { artifact = "com.google.protobuf:protoc:$protobufVersion" }
  plugins {
    id("grpc") { artifact = "io.grpc:protoc-gen-grpc-java:$grpcVersion" }
  }
  generateProtoTasks {
    all().forEach { task ->
      task.plugins { id("grpc") }
    }
  }
}

sourceSets.main {
  proto {
    srcDir("../proto") // point at rpc/proto/qupath.proto
  }
}
