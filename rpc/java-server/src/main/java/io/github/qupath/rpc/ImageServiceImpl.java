package io.github.qupath.rpc;

import qupath.api.ImageServiceGrpc;
import qupath.api.UploadChunk;
import qupath.api.UploadResponse;
import qupath.api.TileRequest;
import qupath.api.TileChunk;

import io.grpc.stub.StreamObserver;

import javax.imageio.ImageIO;
import java.awt.image.BufferedImage;
import java.io.*;
import java.nio.file.Files;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.ConcurrentHashMap;

public class ImageServiceImpl extends ImageServiceGrpc.ImageServiceImplBase {

    private final Map<String, File> slides = new ConcurrentHashMap<>();
    private final int tileSize = 512;

    @Override
    public StreamObserver<UploadChunk> uploadSlide(StreamObserver<UploadResponse> responseObserver) {
        return new StreamObserver<UploadChunk>() {
            private File tmpFile;
            private OutputStream os;

            @Override
            public void onNext(UploadChunk uploadChunk) {
                try {
                    if (tmpFile == null) {
                        tmpFile = Files.createTempFile("qupath_slide_", ".img").toFile();
                        os = new FileOutputStream(tmpFile);
                    }
                    os.write(uploadChunk.getData().toByteArray());
                } catch (IOException e) {
                    onError(e);
                }
            }

            @Override
            public void onError(Throwable t) {
                t.printStackTrace();
                try { if (os != null) os.close(); } catch (IOException ignored) {}
            }

            @Override
            public void onCompleted() {
                try {
                    if (os != null) os.close();
                    // attempt to determine file type and rename to png if possible
                    String slideId = UUID.randomUUID().toString();
                    slides.put(slideId, tmpFile);
                    var resp = UploadResponse.newBuilder().setSlideId(slideId).build();
                    responseObserver.onNext(resp);
                    responseObserver.onCompleted();
                } catch (IOException e) {
                    responseObserver.onError(e);
                }
            }
        };
    }

    @Override
    public void getImageMeta(qupath.api.ImageId request, StreamObserver<qupath.api.ImageMeta> responseObserver) {
        var id = request.getId();
        File f = slides.get(id);
        if (f == null || !f.exists()) {
            responseObserver.onError(new FileNotFoundException("Slide not found"));
            return;
        }
        try {
            BufferedImage img = ImageIO.read(f);
            var meta = qupath.api.ImageMeta.newBuilder()
                    .setId(id)
                    .setWidth(img.getWidth())
                    .setHeight(img.getHeight())
                    .setTileSize(tileSize)
                    .setPixelFormat("RGBA")
                    .build();
            responseObserver.onNext(meta);
            responseObserver.onCompleted();
        } catch (IOException e) {
            responseObserver.onError(e);
        }
    }

    @Override
    public void streamTile(TileRequest request, StreamObserver<TileChunk> responseObserver) {
        var id = request.getImage().getId();
        File f = slides.get(id);
        if (f == null || !f.exists()) {
            responseObserver.onError(new FileNotFoundException("Slide not found"));
            return;
        }
        try {
            BufferedImage img = ImageIO.read(f);
            int tx = request.getX();
            int ty = request.getY();
            int x = tx * tileSize;
            int y = ty * tileSize;
            int w = Math.min(tileSize, img.getWidth() - x);
            int h = Math.min(tileSize, img.getHeight() - y);
            if (w <= 0 || h <= 0) {
                responseObserver.onCompleted();
                return;
            }
            BufferedImage sub = img.getSubimage(x, y, w, h);
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            ImageIO.write(sub, "png", baos);
            var chunk = TileChunk.newBuilder()
                    .setData(com.google.protobuf.ByteString.copyFrom(baos.toByteArray()))
                    .setX(tx)
                    .setY(ty)
                    .setWidth(w)
                    .setHeight(h)
                    .build();
            responseObserver.onNext(chunk);
            responseObserver.onCompleted();
        } catch (IOException e) {
            responseObserver.onError(e);
        }
    }
}
