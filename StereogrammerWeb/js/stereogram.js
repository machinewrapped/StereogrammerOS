// StereogrammerWeb — Horoptic Algorithm
// Ported from StereogrammerOS (C# / WPF)
// Horoptic Algorithm Copyright 1996-2012 Simon Booth
// http://machinewrapped.wordpress.com/stereogrammer/

/**
 * Generate a stereogram using the Horoptic algorithm.
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
 * @returns {ImageData} The generated stereogram
 */
export function generateStereogram(opts) {
    const t0 = performance.now();

    const {
        depthBytes, depthWidth, depthHeight,
        texturePixels, textureWidth, textureHeight,
        outputWidth, outputHeight,
        separationRatio = 0.1,
        fieldDepth: rawFieldDepth = 0.3333,
        removeHidden = false,
        convergenceDots = false
    } = opts;

    const lineWidth = outputWidth;
    const rows = outputHeight;
    const separation = Math.round(separationRatio * lineWidth);
    const fieldDepth = Math.max(0, Math.min(1, rawFieldDepth));
    const midpoint = Math.floor(lineWidth / 2);

    // Output pixel buffer (RGBA packed as Uint32)
    const pixels = new Uint32Array(lineWidth * rows);

    // Pre-compute centre-out index array
    const centreOut = new Int32Array(lineWidth);
    let offset = midpoint;
    let flip = -1;
    for (let i = 0; i < lineWidth; i++) {
        centreOut[i] = offset;
        offset += (i + 1) * flip;
        flip = -flip;
    }

    // Helper: stereo separation at depth Z
    function sep(Z) {
        if (Z < 0) Z = 0;
        if (Z > 1) Z = 1;
        return (1 - fieldDepth * Z) * (2 * separation) / (2 - fieldDepth * Z);
    }

    // Helper: read depth as float 0..1
    function getDepthFloat(x, y) {
        // Scale x,y from output coords to depth coords
        const dx = Math.floor(x * depthWidth / lineWidth);
        const dy = Math.floor(y * depthHeight / rows);
        return depthBytes[dy * depthWidth + dx] / 255;
    }

    // Helper: get tiled texture pixel
    function getTexturePixel(x, y) {
        const tx = ((x + midpoint) % textureWidth + textureWidth) % textureWidth;
        const ty = ((y % textureHeight) + textureHeight) % textureHeight;
        return texturePixels[ty * textureWidth + tx];
    }

    // Helper: outermost of two values relative to midpoint
    function outermost(a, b) {
        return Math.abs(midpoint - a) > Math.abs(midpoint - b) ? a : b;
    }

    // Process each row
    for (let y = 0; y < rows; y++) {
        const constraints = new Int32Array(lineWidth);
        const depthLine = new Float32Array(lineWidth);

        for (let i = 0; i < lineWidth; i++) {
            constraints[i] = i;
            depthLine[i] = getDepthFloat(i, y);
        }

        let maxDepth = 0;

        for (let ii = 0; ii < lineWidth; ii++) {
            const i = centreOut[ii];

            // Horopter Z at this x
            const hArg = (20 * separation) * (20 * separation) - (i - midpoint) * (i - midpoint);
            const ZH_raw = Math.sqrt(Math.max(0, hArg));
            const ZH = 1 - ZH_raw / (20 * separation);

            const s = Math.round(sep(depthLine[i] - ZH / fieldDepth));

            const left = i - Math.floor(s / 2);
            const right = left + s;

            if (left >= 0 && right < lineWidth) {
                let visible = true;

                if (removeHidden) {
                    let t = 1;
                    let zt = depthLine[i];
                    const delta = 2 * (2 - fieldDepth * depthLine[i]) / (fieldDepth * separation * 2);
                    do {
                        zt += delta;
                        if (i - t >= 0 && i + t < lineWidth) {
                            visible = depthLine[i - t] < zt && depthLine[i + t] < zt;
                        }
                        t++;
                    } while (visible && zt < maxDepth);
                }

                if (visible) {
                    let constrainee = outermost(left, right);
                    let constrainer = constrainee === left ? right : left;

                    while (constraints[constrainer] !== constrainer) {
                        constrainer = constraints[constrainer];
                    }

                    constraints[constrainee] = constrainer;
                }

                if (depthLine[i] > maxDepth) {
                    maxDepth = depthLine[i];
                }
            }
        }

        // Resolve constraints and assign texture pixels
        const rowOffset = y * lineWidth;
        for (let i = 0; i < lineWidth; i++) {
            let pix = i;
            while (constraints[pix] !== pix) {
                pix = constraints[pix];
            }
            pixels[rowOffset + i] = getTexturePixel(pix, y);
        }
    }

    // Build ImageData from pixel buffer
    const imageData = new ImageData(lineWidth, rows);
    const view = new Uint32Array(imageData.data.buffer);
    view.set(pixels);

    // Draw convergence dots
    if (convergenceDots) {
        drawConvergenceDots(imageData, midpoint, separation, lineWidth, rows);
    }

    const elapsed = performance.now() - t0;
    return { imageData, elapsed };
}

/**
 * Draw two small filled circles as convergence guides near the top of the image.
 */
function drawConvergenceDots(imageData, midpoint, separation, width, height) {
    const cx1 = Math.floor(midpoint - separation / 2);
    const cx2 = Math.floor(midpoint + separation / 2);
    const cy = Math.floor(height / 16);
    const r = Math.max(2, Math.floor(separation / 16));
    const black = 0xFF000000; // ABGR order when writing Uint32 (opaque black in RGBA view)

    const view = new Uint32Array(imageData.data.buffer);

    for (let dy = -r; dy <= r; dy++) {
        for (let dx = -r; dx <= r; dx++) {
            if (dx * dx + dy * dy <= r * r) {
                const y = cy + dy;
                if (y < 0 || y >= height) continue;

                const x1 = cx1 + dx;
                if (x1 >= 0 && x1 < width) {
                    view[y * width + x1] = black;
                }

                const x2 = cx2 + dx;
                if (x2 >= 0 && x2 < width) {
                    view[y * width + x2] = black;
                }
            }
        }
    }
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
