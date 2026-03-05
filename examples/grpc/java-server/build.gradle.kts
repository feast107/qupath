plugins {
  application
  id("com.google.protobuf") version "0.9.4"
}

repositories {
  mavenCentral()
}

val grpcVersion = "1.56.0"
val protobufVersion = "3.21.12"

dependencies {
  implementation("io.grpc:grpc-netty-shaded:$grpcVersion")
  implementation("io.grpc:grpc-protobuf:$grpcVersion")
  implementation("io.grpc:grpc-stub:$grpcVersion")
  implementation("com.google.protobuf:protobuf-java:$protobufVersion")
}

application {
  mainClass.set("qupath.grpc.ServerMain")
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
