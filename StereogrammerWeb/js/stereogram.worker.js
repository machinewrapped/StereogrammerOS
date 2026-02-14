// StereogrammerWeb — Horoptic Algorithm (Worker)
// Handles heavy lifting of stereogram generation in a background thread

self.onmessage = function(e) {
    const {
        depthBytes, depthWidth, depthHeight,
        texturePixels, textureWidth, textureHeight,
        outputWidth, outputHeight,
        separationRatio,
        fieldDepth: rawFieldDepth,
        removeHidden,
        convergenceDots
    } = e.data;

    try {
        const t0 = performance.now();

        const lineWidth = outputWidth;
        const rows = outputHeight;
        const separation = Math.round(separationRatio * lineWidth);
        const fieldDepth = Math.max(0, Math.min(1, rawFieldDepth));
        const midpoint = Math.floor(lineWidth / 2);

        // Output pixel buffer (RGBA packed as Uint32)
        const pixels = new Uint32Array(lineWidth * rows);

        // Pre-compute centre-out index array (reused)
        const centreOut = new Int32Array(lineWidth);
        let offset = midpoint;
        let flip = -1;
        for (let i = 0; i < lineWidth; i++) {
            centreOut[i] = offset;
            offset += (i + 1) * flip;
            flip = -flip;
        }

        // Optimization: Pre-allocate reusable arrays for row processing
        // These are typed arrays, so allocation is relatively cheap, but reusing them 
        // across rows saves GC pressure.
        const constraints = new Int32Array(lineWidth);
        const depthLine = new Float32Array(lineWidth);

        // Constants used in the loop
        const invertedTextureWidth = 1 / textureWidth; // optimization if needed, but modulo is fast enough usually
        
        // Report progress every 5%
        const progressStep = Math.ceil(rows / 20);

        // Helper: stereo separation at depth Z
        // Inlined or closures are fine in modern JS engines
        const sep = (Z) => {
            if (Z < 0) Z = 0;
            if (Z > 1) Z = 1;
            return (1 - fieldDepth * Z) * (2 * separation) / (2 - fieldDepth * Z);
        };

        // Process each row
        for (let y = 0; y < rows; y++) {
            // Report progress
            if (y % progressStep === 0) {
                self.postMessage({ type: 'progress', value: y / rows });
            }

            // Fill arrays for this row efficiently
            for (let i = 0; i < lineWidth; i++) {
                constraints[i] = i;
                
                // Scale x,y from output coords to depth coords (nearest neighbor)
                const dx = Math.floor(i * depthWidth / lineWidth);
                const dy = Math.floor(y * depthHeight / rows);
                
                // Read depth byte and normalize to 0..1
                depthLine[i] = depthBytes[dy * depthWidth + dx] / 255;
            }

            let maxDepth = 0;
            const separationFactor = 20 * separation;
            const separationFactorSq = separationFactor * separationFactor;

            // Calculate constraints based on depth
            for (let ii = 0; ii < lineWidth; ii++) {
                const i = centreOut[ii];
                const iDist = i - midpoint;

                // Horopter Z at this x
                const hArg = separationFactorSq - iDist * iDist;
                const ZH_raw = Math.sqrt(Math.max(0, hArg));
                const ZH = 1 - ZH_raw / separationFactor;

                const s = Math.round(sep(depthLine[i] - ZH / fieldDepth));

                const left = i - Math.floor(s / 2);
                const right = left + s;

                if (left >= 0 && right < lineWidth) {
                    let visible = true;

                    if (removeHidden) {
                        let t = 1;
                        let zt = depthLine[i];
                        const delta = 2 * (2 - fieldDepth * depthLine[i]) / (fieldDepth * separation * 2);
                        
                        // Look for occlusion
                        // We limit the search to avoid infinite loops in pathological cases, 
                        // though the algorithm usually converges.
                        const maxSearch = Math.max(separation, 100); 
                        
                        while (visible && zt < maxDepth && t < maxSearch) {
                            zt += delta;
                            const checkLeft = i - t >= 0 && depthLine[i - t] < zt;
                            const checkRight = i + t < lineWidth && depthLine[i + t] < zt;
                            
                            if ((i - t >= 0 && !checkLeft) || (i + t < lineWidth && !checkRight)) {
                                visible = false;
                            } else if (i - t >= 0 && i + t < lineWidth) {
                                // Both exist, check strictly
                                if (!(depthLine[i - t] < zt && depthLine[i + t] < zt)) {
                                    visible = false;
                                }
                            }
                            t++;
                        }
                    }

                    if (visible) {
                        const distLeft = Math.abs(midpoint - left);
                        const distRight = Math.abs(midpoint - right);
                        
                        const constrainee = distLeft > distRight ? left : right;
                        let constrainer = constrainee === left ? right : left;

                        // Link following (union-find style path compression could be added but might be overkill)
                        let temp = constrainer;
                        let loopCount = 0;
                        while (constraints[temp] !== temp && loopCount < lineWidth) {
                            temp = constraints[temp];
                            loopCount++;
                        }
                        constrainer = temp;

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
                let loopCount = 0;
                while (constraints[pix] !== pix && loopCount < lineWidth) {
                    pix = constraints[pix];
                    loopCount++;
                }
                
                // Get texture pixel
                const tx = ((pix + midpoint) % textureWidth + textureWidth) % textureWidth;
                const ty = ((y % textureHeight) + textureHeight) % textureHeight;
                const texPixel = texturePixels[ty * textureWidth + tx];

                pixels[rowOffset + i] = texPixel;
            }
        }

        // Draw convergence dots
        if (convergenceDots) {
            drawConvergenceDots(pixels, midpoint, separation, lineWidth, rows);
        }

        const elapsed = performance.now() - t0;
        
        // Post result back
        self.postMessage({
            type: 'done',
            pixels: pixels, // Transferable if we want, but copying is fine for one frame
            elapsed: elapsed
        }, [pixels.buffer]); // Transfer the buffer to save memory

    } catch (err) {
        self.postMessage({ type: 'error', message: err.message });
    }
};

function drawConvergenceDots(pixels, midpoint, separation, width, height) {
    const cx1 = Math.floor(midpoint - separation / 2);
    const cx2 = Math.floor(midpoint + separation / 2);
    const cy = Math.floor(height / 16);
    const r = Math.max(2, Math.floor(separation / 16));
    const black = 0xFF000000;

    for (let dy = -r; dy <= r; dy++) {
        for (let dx = -r; dx <= r; dx++) {
            if (dx * dx + dy * dy <= r * r) {
                const y = cy + dy;
                if (y < 0 || y >= height) continue;

                const x1 = cx1 + dx;
                if (x1 >= 0 && x1 < width) {
                    pixels[y * width + x1] = black;
                }

                const x2 = cx2 + dx;
                if (x2 >= 0 && x2 < width) {
                    pixels[y * width + x2] = black;
                }
            }
        }
    }
}
