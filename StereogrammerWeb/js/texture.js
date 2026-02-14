// StereogrammerWeb — Texture generation and image helpers

/**
 * Generate a random greyscale dot texture.
 * @param {number} width
 * @param {number} height
 * @returns {Uint32Array} RGBA-packed pixels
 */
export function generateGreyDots(width, height) {
    const pixels = new Uint32Array(width * height);
    for (let i = 0; i < pixels.length; i++) {
        const v = Math.floor(Math.random() * 256);
        // RGBA packed as 0xAABBGGRR (little-endian Uint32)
        pixels[i] = 0xFF000000 | (v << 16) | (v << 8) | v;
    }
    return pixels;
}

/**
 * Generate a random colour dot texture.
 * @param {number} width
 * @param {number} height
 * @returns {Uint32Array} RGBA-packed pixels
 */
export function generateColourDots(width, height) {
    const pixels = new Uint32Array(width * height);
    for (let i = 0; i < pixels.length; i++) {
        const r = Math.floor(Math.random() * 256);
        const g = Math.floor(Math.random() * 256);
        const b = Math.floor(Math.random() * 256);
        pixels[i] = 0xFF000000 | (b << 16) | (g << 8) | r;
    }
    return pixels;
}

/**
 * Create a thumbnail data URL from an image element.
 * @param {HTMLImageElement} img
 * @param {number} size – Max dimension
 * @returns {string} Data URL (PNG)
 */
export function createThumbnail(img, size = 80) {
    const canvas = document.createElement('canvas');
    const aspect = img.naturalWidth / img.naturalHeight;
    if (aspect >= 1) {
        canvas.width = size;
        canvas.height = Math.round(size / aspect);
    } else {
        canvas.height = size;
        canvas.width = Math.round(size * aspect);
    }
    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
    return canvas.toDataURL('image/png');
}

/**
 * Load an image from a data URL or object URL.
 * @param {string} src
 * @returns {Promise<HTMLImageElement>}
 */
export function loadImage(src) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = () => reject(new Error('Failed to load image'));
        img.src = src;
    });
}

/**
 * Read a File as a data URL.
 * @param {File} file
 * @returns {Promise<string>}
 */
export function readFileAsDataURL(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = () => reject(reader.error);
        reader.readAsDataURL(file);
    });
}
