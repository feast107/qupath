package qupath.rpc;

import io.grpc.Server;
import io.grpc.ServerBuilder;
import io.grpc.stub.StreamObserver;
import qupath.api.ImageServiceGrpc;
import qupath.api.ImageId;
import qupath.api.ImageMeta;
import qupath.api.TileRequest;
import qupath.api.TileChunk;

import java.io.IOException;
import java.nio.charset.StandardCharsets;

public class ImageServer {
    public static void main(String[] args) throws IOException, InterruptedException {
        Server server = ServerBuilder.forPort(50051)
                .addService(new ImageServiceImpl())
                .build()
                .start();
        System.out.println("gRPC server started on port 50051");
        server.awaitTermination();
    }

    static class ImageServiceImpl extends ImageServiceGrpc.ImageServiceImplBase {
        @Override
        public void getImageMeta(ImageId request, StreamObserver<ImageMeta> responseObserver) {
            ImageMeta meta = ImageMeta.newBuilder()
                    .setId(request.getId())
                    .setWidth(10000)
                    .setHeight(8000)
                    .addPyramidLevels(0)
                    .addPyramidLevels(1)
                    .setTileSize(512)
                    .setPixelFormat("BGR")
                    .build();
            responseObserver.onNext(meta);
            responseObserver.onCompleted();
        }

        @Override
        public void streamTile(TileRequest request, StreamObserver<TileChunk> responseObserver) {
            // Example: send 3 trivial chunks for demo
            for (int i = 0; i < 3; i++) {
                byte[] payload = ("tile-" + request.getImage().getId() + "-" + request.getLevel() + "-" + request.getX() + "-" + request.getY() + "-chunk" + i).getBytes(StandardCharsets.UTF_8);
                TileChunk chunk = TileChunk.newBuilder()
                        .setData(com.google.protobuf.ByteString.copyFrom(payload))
                        .setFinalChunk(i == 2)
                        .setChunkIndex(i)
                        .build();
                responseObserver.onNext(chunk);
            }
            responseObserver.onCompleted();
        }
    }
}
