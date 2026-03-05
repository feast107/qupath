package qupath.grpc;

import io.grpc.Server;
import io.grpc.ServerBuilder;
import io.grpc.stub.StreamObserver;
import com.google.protobuf.ByteString;
import io.github.qupath.api.ImageServiceGrpc;
import io.github.qupath.api.ImageId;
import io.github.qupath.api.ImageMeta;
import io.github.qupath.api.TileRequest;
import io.github.qupath.api.TileChunk;

import java.io.IOException;

public class ServerMain {

    public static class SimpleImageService extends ImageServiceGrpc.ImageServiceImplBase {
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
            // Example: stream a single small fake tile as a demo. Real server should stream real bytes.
            byte[] fake = new byte[request.getImage().getId().length() + 16];
            for (int i = 0; i < fake.length; i++) fake[i] = (byte)(i & 0xff);
            TileChunk chunk = TileChunk.newBuilder()
                    .setData(ByteString.copyFrom(fake))
                    .setFinalChunk(true)
                    .setChunkIndex(0)
                    .setWidth(128)
                    .setHeight(128)
                    .build();
            responseObserver.onNext(chunk);
            responseObserver.onCompleted();
        }
    }

    public static void main(String[] args) throws IOException, InterruptedException {
        int port = 50051;
        Server server = ServerBuilder.forPort(port)
                .addService(new SimpleImageService())
                .build()
                .start();
        System.out.println("gRPC server started on port " + port);
        Runtime.getRuntime().addShutdownHook(new Thread(server::shutdown));
        server.awaitTermination();
    }
}
