// StereogrammerWeb — Application entry point and UI orchestration

import { generateStereogram, extractDepthData, extractTextureData } from './stereogram.js';
import { generateGreyDots, generateColourDots, createThumbnail, loadImage, readFileAsDataURL } from './texture.js';
import { loadTextureLibrary, addTexture, removeTexture } from './storage.js';

// ── State ──────────────────────────────────────────────────────
let state = {
    depthMapImg: null,          // HTMLImageElement
    depthMapDataUrl: null,
    selectedTexture: null,      // { type, name, img?, dataUrl?, index? }
    separationRatio: 0.1,
    fieldDepth: 0.3333,
    removeHidden: true,
    convergenceDots: true,
    scaleMode: 'viewport',      // 'viewport' | 'fixed'
    fixedWidth: 1920,
    fixedHeight: 1080,
};

// ── DOM refs ───────────────────────────────────────────────────
const $ = (sel) => document.querySelector(sel);
const outputCanvas   = $('#output-canvas');
const outputCtx      = outputCanvas.getContext('2d');

// Controls
const depthInput       = $('#depth-upload');
const depthPreview     = $('#depth-preview');
const depthLabel       = $('#depth-label');
const sepSlider        = $('#sep-slider');
const sepValue         = $('#sep-value');
const depthSlider      = $('#depth-slider');
const depthValue       = $('#depth-value');
const hiddenCheck      = $('#hidden-check');
const dotsCheck        = $('#dots-check');
const scaleModeRadios  = document.querySelectorAll('input[name="scale-mode"]');
const fixedControls    = $('#fixed-controls');
const fixedWidthInput  = $('#fixed-width');
const fixedHeightInput = $('#fixed-height');
const generateBtn      = $('#generate-btn');
const saveBtn          = $('#save-btn');
const statusEl         = $('#status-message');
const progressBar      = $('#progress-bar');
const progressContainer= $('#progress-container');

// Texture panel
const textureGrid    = $('#texture-grid');
const textureInput   = $('#texture-upload');
const addTextureBtn  = $('#add-texture-btn');

// Export dialog
const exportOverlay  = $('#export-overlay');
const exportWidth    = $('#export-width');
const exportHeight   = $('#export-height');
const exportGenerate = $('#export-generate');
const exportCancel   = $('#export-cancel');

// ── Init ───────────────────────────────────────────────────────
function init() {
    // Wire controls
    depthInput.addEventListener('change', onDepthUpload);
    sepSlider.addEventListener('input', () => {
        state.separationRatio = parseFloat(sepSlider.value);
        sepValue.textContent = sepSlider.value;
    });
    depthSlider.addEventListener('input', () => {
        state.fieldDepth = parseFloat(depthSlider.value);
        depthValue.textContent = parseFloat(depthSlider.value).toFixed(2);
    });
    hiddenCheck.addEventListener('change', () => state.removeHidden = hiddenCheck.checked);
    dotsCheck.addEventListener('change', () => state.convergenceDots = dotsCheck.checked);

    scaleModeRadios.forEach(r => r.addEventListener('change', () => {
        state.scaleMode = document.querySelector('input[name="scale-mode"]:checked').value;
        fixedControls.classList.toggle('hidden', state.scaleMode !== 'fixed');
    }));
    fixedWidthInput.addEventListener('change', () => {
        state.fixedWidth = parseInt(fixedWidthInput.value) || 1920;
        // Maintain 16:9
        state.fixedHeight = Math.round(state.fixedWidth * 9 / 16);
        fixedHeightInput.value = state.fixedHeight;
    });
    fixedHeightInput.addEventListener('change', () => {
        state.fixedHeight = parseInt(fixedHeightInput.value) || 1080;
        state.fixedWidth = Math.round(state.fixedHeight * 16 / 9);
        fixedWidthInput.value = state.fixedWidth;
    });

    generateBtn.addEventListener('click', onGenerate);
    saveBtn.addEventListener('click', onSave);

    addTextureBtn.addEventListener('click', () => textureInput.click());
    textureInput.addEventListener('change', onTextureUpload);

    // Export dialog
    exportGenerate.addEventListener('click', onExportGenerate);
    exportCancel.addEventListener('click', () => exportOverlay.classList.add('hidden'));

    // Set initial control values
    sepSlider.value = state.separationRatio;
    sepValue.textContent = state.separationRatio;
    depthSlider.value = state.fieldDepth;
    depthValue.textContent = state.fieldDepth.toFixed(2);
    dotsCheck.checked = state.convergenceDots;
    fixedWidthInput.value = state.fixedWidth;
    fixedHeightInput.value = state.fixedHeight;

    // Render texture library
    renderTextureLibrary();

    // Select grey dots by default
    selectTexture({ type: 'grey-dots', name: 'Grey Dots' });

    // Responsive canvas sizing
    window.addEventListener('resize', resizeCanvas);
    resizeCanvas();
}

// ── Depth Map Upload ───────────────────────────────────────────
async function onDepthUpload(e) {
    const file = e.target.files[0];
    if (!file) return;

    try {
        const dataUrl = await readFileAsDataURL(file);
        const img = await loadImage(dataUrl);

        state.depthMapImg = img;
        state.depthMapDataUrl = dataUrl;

        depthPreview.src = dataUrl;
        depthPreview.classList.remove('hidden');
        depthLabel.textContent = file.name;

        // Hide placeholder elements
        const uploadArea = depthPreview.closest('.depth-upload-area');
        uploadArea.querySelector('.upload-icon').classList.add('hidden');
        uploadArea.querySelector('.upload-text').classList.add('hidden');

        setStatus('Depth map loaded — ready to generate');
        generateBtn.disabled = false;
    } catch (err) {
        setStatus('Error loading depth map: ' + err.message);
    }
}

// ── Texture Library ────────────────────────────────────────────
function renderTextureLibrary() {
    textureGrid.innerHTML = '';

    // Built-in textures
    const builtIn = [
        { type: 'grey-dots', name: 'Grey Dots', class: 'grey-dots-preview' },
        { type: 'colour-dots', name: 'Colour Dots', class: 'colour-dots-preview' },
    ];

    builtIn.forEach(tex => {
        const card = createTextureCard(tex.name, null, tex.class, tex.type === state.selectedTexture?.type);
        card.addEventListener('click', () => selectTexture(tex));
        textureGrid.appendChild(card);
    });

    // User textures
    const library = loadTextureLibrary();
    library.forEach((tex, idx) => {
        const card = createTextureCard(
            tex.name,
            tex.thumbnail,
            null,
            state.selectedTexture?.type === 'user' && state.selectedTexture?.index === idx
        );
        card.addEventListener('click', () => selectTexture({ type: 'user', name: tex.name, dataUrl: tex.dataUrl, index: idx }));

        // Remove button
        const removeBtn = document.createElement('button');
        removeBtn.className = 'remove-texture-btn';
        removeBtn.textContent = '×';
        removeBtn.title = 'Remove texture';
        removeBtn.addEventListener('click', (ev) => {
            ev.stopPropagation();
            removeTexture(idx);
            if (state.selectedTexture?.type === 'user' && state.selectedTexture?.index === idx) {
                selectTexture({ type: 'grey-dots', name: 'Grey Dots' });
            }
            renderTextureLibrary();
        });
        card.appendChild(removeBtn);

        textureGrid.appendChild(card);
    });
}

function createTextureCard(name, thumbnailUrl, cssClass, isSelected) {
    const card = document.createElement('div');
    card.className = 'texture-card' + (isSelected ? ' selected' : '');

    const preview = document.createElement('div');
    preview.className = 'texture-preview' + (cssClass ? ' ' + cssClass : '');
    if (thumbnailUrl) {
        preview.style.backgroundImage = `url(${thumbnailUrl})`;
        preview.style.backgroundSize = 'cover';
    }

    const label = document.createElement('span');
    label.className = 'texture-label';
    label.textContent = name;

    card.appendChild(preview);
    card.appendChild(label);
    return card;
}

function selectTexture(tex) {
    state.selectedTexture = tex;
    renderTextureLibrary();
}

async function onTextureUpload(e) {
    const file = e.target.files[0];
    if (!file) return;

    try {
        const dataUrl = await readFileAsDataURL(file);
        const img = await loadImage(dataUrl);
        const thumbnail = createThumbnail(img, 80);
        const name = file.name.replace(/\.[^.]+$/, '');

        addTexture(name, dataUrl, thumbnail);
        renderTextureLibrary();
        setStatus(`Texture "${name}" added to library`);
    } catch (err) {
        setStatus('Error loading texture: ' + err.message);
    }

    textureInput.value = '';
}

// ── Stereogram Generation ──────────────────────────────────────
async function onGenerate() {
    if (!state.depthMapImg) {
        setStatus('Please upload a depth map first');
        return;
    }
    if (!state.selectedTexture) {
        setStatus('Please select a texture');
        return;
    }

    let outputWidth, outputHeight;
    if (state.scaleMode === 'viewport') {
        const container = $('#output-area');
        outputWidth = Math.floor(container.clientWidth - 48); // account for padding
        outputHeight = Math.round(outputWidth * 9 / 16);
    } else {
        outputWidth = state.fixedWidth;
        outputHeight = state.fixedHeight;
    }

    await generate(outputWidth, outputHeight, true);
}

function updateProgress(value) {
    progressBar.style.width = `${value * 100}%`;
}

async function generate(outputWidth, outputHeight, displayOnCanvas) {
    setStatus('Generating...');
    generateBtn.disabled = true;
    progressContainer.classList.remove('hidden');
    updateProgress(0);

    try {
        const separation = Math.round(state.separationRatio * outputWidth);

        // Prepare depth data
        const { depthBytes, depthWidth, depthHeight } = extractDepthData(
            state.depthMapImg, outputWidth, outputHeight
        );

        // Prepare texture data
        const tileWidth = separation;
        const tileHeight = await getTextureHeight(tileWidth);
        const texturePixels = await getTexturePixels(tileWidth, tileHeight);

        // Generate
        const { imageData, elapsed } = await generateStereogram({
            depthBytes, depthWidth, depthHeight,
            texturePixels, textureWidth: tileWidth, textureHeight: tileHeight,
            outputWidth, outputHeight,
            separationRatio: state.separationRatio,
            fieldDepth: state.fieldDepth,
            removeHidden: state.removeHidden,
            convergenceDots: state.convergenceDots,
        }, updateProgress);

        if (displayOnCanvas) {
            outputCanvas.width = outputWidth;
            outputCanvas.height = outputHeight;
            outputCtx.putImageData(imageData, 0, 0);
            saveBtn.disabled = false;
        }

        setStatus(`Generated in ${elapsed.toFixed(0)} ms (${outputWidth}×${outputHeight})`);
        return imageData;
    } catch (err) {
        setStatus('Error: ' + err.message);
        console.error(err);
        return null;
    } finally {
        generateBtn.disabled = false;
        setTimeout(() => {
            if (generateBtn.disabled) return;
            progressContainer.classList.add('hidden');
            updateProgress(0);
        }, 500);
    }
}

async function getTextureHeight(tileWidth) {
    const tex = state.selectedTexture;
    if (tex.type === 'grey-dots' || tex.type === 'colour-dots') {
        return tileWidth; // Square tile
    }
    // User texture: preserve aspect ratio
    const img = await loadImage(tex.dataUrl);
    return Math.round(tileWidth * img.naturalHeight / img.naturalWidth);
}

async function getTexturePixels(tileWidth, tileHeight) {
    const tex = state.selectedTexture;
    if (tex.type === 'grey-dots') {
        return generateGreyDots(tileWidth, tileHeight);
    }
    if (tex.type === 'colour-dots') {
        return generateColourDots(tileWidth, tileHeight);
    }
    // User texture
    const img = await loadImage(tex.dataUrl);
    const { texturePixels } = extractTextureData(img, tileWidth, tileHeight);
    return texturePixels;
}

// ── Save / Export ──────────────────────────────────────────────
function onSave() {
    if (state.scaleMode === 'viewport') {
        // Show export dialog for resolution choice
        exportWidth.value = 1920;
        exportHeight.value = 1080;
        exportOverlay.classList.remove('hidden');
    } else {
        downloadCanvas();
    }
}

async function onExportGenerate() {
    const w = parseInt(exportWidth.value) || 1920;
    const h = parseInt(exportHeight.value) || 1080;
    exportOverlay.classList.add('hidden');

    const imageData = await generate(w, h, false);
    if (!imageData) return;

    // Draw to an offscreen canvas and download
    const exportCanvas = document.createElement('canvas');
    exportCanvas.width = w;
    exportCanvas.height = h;
    exportCanvas.getContext('2d').putImageData(imageData, 0, 0);

    const link = document.createElement('a');
    link.download = 'stereogram.png';
    link.href = exportCanvas.toDataURL('image/png');
    link.click();
    setStatus(`Exported ${w}×${h} PNG`);
}

function downloadCanvas() {
    const link = document.createElement('a');
    link.download = 'stereogram.png';
    link.href = outputCanvas.toDataURL('image/png');
    link.click();
}

// ── Helpers ────────────────────────────────────────────────────
function setStatus(msg) {
    statusEl.textContent = msg;
}

function resizeCanvas() {
    if (state.scaleMode === 'viewport' && outputCanvas.width > 0) {
        // Just let CSS handle the display scaling
    }
}

// ── Bootstrap ──────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', init);
