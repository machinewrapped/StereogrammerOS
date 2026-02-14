// StereogrammerWeb — Horoptic Algorithm
// Ported from StereogrammerOS (C# / WPF)
// Horoptic Algorithm Copyright 1996-2012 Simon Booth
// http://machinewrapped.wordpress.com/stereogrammer/

/**
 * Generate a stereogram asynchronously using a Web Worker.
 *
 * @param {object} opts
 * @param {Uint8Array} opts.depthBytes      – Greyscale depth data, 1 byte/pixel, row-major
 * @param {number}     opts.depthWidth      – Width of the depth data
 * @param {number}     opts.depthHeight     – Height of the depth data
 * @param {Uint32Array} opts.texturePixels  – RGBA packed texture pixels
 * @param {number}     opts.textureWidth    – Width of the texture tile
 * @param {number}     opts.textureHeight   – Height of the texture tile
 * @param {number}     opts.outputWidth     – Output image width
 * @param {number}     opts.outputHeight    – Output image height
 * @param {number}     opts.separationRatio – Separation as fraction of width (e.g. 0.1)
 * @param {number}     opts.fieldDepth      – Depth factor 0–1 (default 0.333)
 * @param {boolean}    opts.removeHidden    – Remove hidden surfaces
 * @param {boolean}    opts.convergenceDots – Draw convergence guide dots
 * @param {function}   [onProgress]         - Optional callback (0..1)
 * @returns {Promise<{imageData: ImageData, elapsed: number}>} The generated stereogram
 * @description Note: depthBytes and texturePixels buffers are transferred to the worker and will be detached/unusable in the main thread.
 */
export function generateStereogram(opts, onProgress) {
    return new Promise((resolve, reject) => {
        const worker = new Worker(new URL('stereogram.worker.js', import.meta.url));

        // Prepare message payload
        const message = {
            depthBytes: opts.depthBytes,
            depthWidth: opts.depthWidth,
            depthHeight: opts.depthHeight,
            texturePixels: opts.texturePixels,
            textureWidth: opts.textureWidth,
            textureHeight: opts.textureHeight,
            outputWidth: opts.outputWidth,
            outputHeight: opts.outputHeight,
            separationRatio: opts.separationRatio,
            fieldDepth: opts.fieldDepth,
            removeHidden: opts.removeHidden,
            convergenceDots: opts.convergenceDots
        };

        // Transferables to save memory/copying
        const transferables = [
            opts.depthBytes.buffer,
            opts.texturePixels.buffer
        ];

        worker.onmessage = function(e) {
            const data = e.data;
            if (data.type === 'progress') {
                if (onProgress) onProgress(data.value);
            } else if (data.type === 'done') {
                const pixels = data.pixels; // this is a Uint32Array
                
                // Convert back to ImageData
                const imageData = new ImageData(new Uint8ClampedArray(pixels.buffer), opts.outputWidth, opts.outputHeight);
                
                worker.terminate();
                resolve({ 
                    imageData: imageData, 
                    elapsed: data.elapsed 
                });
            } else if (data.type === 'error') {
                worker.terminate();
                reject(new Error(data.message));
            }
        };

        worker.onerror = function(err) {
            worker.terminate();
            reject(err);
        };

        worker.postMessage(message, transferables);
    });
}

/**
 * Load an image into a canvas, convert to greyscale, and return depth data.
 *
 * @param {HTMLImageElement|ImageBitmap} img
 * @param {number} targetWidth
 * @param {number} targetHeight
 * @returns {{ depthBytes: Uint8Array, depthWidth: number, depthHeight: number }}
 */
export function extractDepthData(img, targetWidth, targetHeight) {
    const canvas = document.createElement('canvas');
    canvas.width = targetWidth;
    canvas.height = targetHeight;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0, targetWidth, targetHeight);

    const imgData = ctx.getImageData(0, 0, targetWidth, targetHeight);
    const data = imgData.data;
    const depthBytes = new Uint8Array(targetWidth * targetHeight);

    for (let i = 0; i < depthBytes.length; i++) {
        const offset = i * 4;
        // Luminance formula
        depthBytes[i] = Math.round(0.299 * data[offset] + 0.587 * data[offset + 1] + 0.114 * data[offset + 2]);
    }

    return { depthBytes, depthWidth: targetWidth, depthHeight: targetHeight };
}

/**
 * Load an image into a canvas and return its pixels as Uint32Array (tile-ready).
 *
 * @param {HTMLImageElement|ImageBitmap} img
 * @param {number} tileWidth   – Desired tile width (= separation in pixels)
 * @param {number} tileHeight  – Calculated to preserve aspect ratio
 * @returns {{ texturePixels: Uint32Array, textureWidth: number, textureHeight: number }}
 */
export function extractTextureData(img, tileWidth, tileHeight) {
    const canvas = document.createElement('canvas');
    canvas.width = tileWidth;
    canvas.height = tileHeight;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0, tileWidth, tileHeight);

    const imgData = ctx.getImageData(0, 0, tileWidth, tileHeight);
    const texturePixels = new Uint32Array(imgData.data.buffer).slice();

    return { texturePixels, textureWidth: tileWidth, textureHeight: tileHeight };
}
