// StereogrammerWeb — LocalStorage wrapper for texture library

const STORAGE_KEY = 'stereogrammer_textures';

/**
 * Load the texture library from localStorage.
 * @returns {Array<{name: string, dataUrl: string, thumbnail: string}>}
 */
export function loadTextureLibrary() {
    try {
        const json = localStorage.getItem(STORAGE_KEY);
        return json ? JSON.parse(json) : [];
    } catch {
        return [];
    }
}

/**
 * Save the texture library to localStorage.
 * @param {Array<{name: string, dataUrl: string, thumbnail: string}>} library
 */
export function saveTextureLibrary(library) {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(library));
    } catch (e) {
        console.warn('Failed to save texture library — storage may be full:', e);
    }
}

/**
 * Add a texture to the library and persist.
 * @param {string} name
 * @param {string} dataUrl – Full-size image data URL
 * @param {string} thumbnail – Thumbnail data URL
 * @returns {Array} Updated library
 */
export function addTexture(name, dataUrl, thumbnail) {
    const library = loadTextureLibrary();
    library.push({ name, dataUrl, thumbnail });
    saveTextureLibrary(library);
    return library;
}

/**
 * Remove a texture by index and persist.
 * @param {number} index
 * @returns {Array} Updated library
 */
export function removeTexture(index) {
    const library = loadTextureLibrary();
    library.splice(index, 1);
    saveTextureLibrary(library);
    return library;
}
