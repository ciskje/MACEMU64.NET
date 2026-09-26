// MACEMU web renderer: text screen + BCF splash/MEMDISPLAY + bell.
// Text grid is 80x50 cells of 16x16px (1280x800 backing), VGA palette.
window.MacEmu = (() => {
  const PAL = [
    [0,0,0],[0,0,170],[0,170,0],[0,170,170],
    [170,0,0],[170,0,170],[170,85,0],[170,170,170],
    [85,85,85],[85,85,255],[85,255,85],[85,255,255],
    [255,85,85],[255,85,255],[255,255,85],[255,255,255],
  ];
  const COLS = 80, ROWS = 50, CELL = 16, W = 1280, H = 800;
  let textC = null, tctx = null, gfxC = null, gctx = null;
  let off = null, offCtx = null, offImg = null;
  let prevChars = "", prevAttrs = null, fontOk = false;
  const bcfCache = {};
  let memBg = null; // { w, h, img: ImageData } for MEMVIEW.BCF
  let audio = null;
  let keyRef = null, keyEl = null;

  function css([r, g, b]) { return `rgb(${r},${g},${b})`; }

  // Blazor JS interop marshals byte[] as a Base64 string.
  function toBytes(a) {
    if (a instanceof Uint8Array) return a;
    const bin = atob(a);
    const b = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) b[i] = bin.charCodeAt(i);
    return b;
  }

  // (Re)acquire canvas contexts: Blazor destroys/recreates the canvas
  // elements when switching text<->graphics branches, so cached refs go stale.
  function ensureCtx() {
    if (!textC || !textC.isConnected) {
      textC = document.getElementById("textCanvas");
      tctx = textC ? textC.getContext("2d") : null;
      prevAttrs = null; prevChars = "";
    }
    if (!gfxC || !gfxC.isConnected) {
      gfxC = document.getElementById("gfxCanvas");
      gctx = gfxC ? gfxC.getContext("2d") : null;
    }
  }

  async function init() {
    ensureCtx();
    off = document.createElement("canvas");
    off.width = 320; off.height = 200;
    offCtx = off.getContext("2d");
    offImg = offCtx.createImageData(320, 200);
    try {
      const face = new FontFace("Px437VGA", "url('fonts/Px437_IBM_EGA_8x8.ttf')");
      await face.load();
      document.fonts.add(face);
      fontOk = true;
    } catch { fontOk = false; }
  }

  function drawText(chars, attrs) {
    ensureCtx();
    if (!tctx) return;
    attrs = toBytes(attrs);
    const full = !prevAttrs || prevAttrs.length !== attrs.length || prevChars.length !== chars.length;
    tctx.font = '16px "Px437VGA", monospace';
    tctx.textBaseline = "top";
    for (let y = 0; y < ROWS; y++) {
      for (let x = 0; x < COLS; x++) {
        const i = y * COLS + x;
        if (!full && chars[i] === prevChars[i] && attrs[i] === prevAttrs[i]) continue;
        const a = attrs[i];
        tctx.fillStyle = css(PAL[(a >> 4) & 7]);
        tctx.fillRect(x * CELL, y * CELL, CELL, CELL);
        const ch = chars[i];
        if (ch !== " " && ch !== "\0") {
          // Clip alle celle 16x16 come il glyph-atlas desktop: senza clip i
          // glifi 16px sbordano sulle righe vicine e il testo si sgrana.
          tctx.fillStyle = css(PAL[a & 15]);
          tctx.save();
          tctx.beginPath();
          tctx.rect(x * CELL, y * CELL, CELL, CELL);
          tctx.clip();
          tctx.fillText(ch, x * CELL, y * CELL);
          tctx.restore();
        }
      }
    }
    prevChars = chars;
    prevAttrs = attrs.slice();
  }

  async function getBcf(url) {
    if (bcfCache[url]) return bcfCache[url];
    const res = await fetch(url);
    const buf = new Uint8Array(await res.arrayBuffer());
    const w = buf[12] | (buf[13] << 8), h = buf[14] | (buf[15] << 8);
    if (buf[16] !== 8) throw new Error("bad BCF " + url);
    const pal = [];
    let o = 18;
    for (let i = 0; i < 256; i++) {
      const b = buf[o++] >> 2, g = buf[o++] >> 2, r = buf[o++] >> 2;
      pal.push([Math.round(r * 255 / 63), Math.round(g * 255 / 63), Math.round(b * 255 / 63)]);
    }
    const px = new Uint8Array(w * h);
    for (let y = h - 1; y >= 0; y--)
      for (let x = 0; x < w; x++)
        px[y * w + x] = buf[o++];
    const bcf = { w, h, pal, px };
    bcfCache[url] = bcf;
    return bcf;
  }

  // 320x200 indexed BCF -> canvas image, upscaled x4 nearest to 1280x800.
  async function drawSplash(url) {
    ensureCtx();
    if (!gctx) return;
    const b = await getBcf(url);
    const img = offCtx.createImageData(b.w, b.h);
    for (let i = 0; i < b.w * b.h; i++) {
      const [r, g, bl] = b.pal[b.px[i]];
      img.data[i * 4] = r; img.data[i * 4 + 1] = g;
      img.data[i * 4 + 2] = bl; img.data[i * 4 + 3] = 255;
    }
    offCtx.putImageData(img, 0, 0);
    gctx.imageSmoothingEnabled = false;
    gctx.drawImage(off, 0, 0, W, H);
  }

  // MEMDISPLAY: MEMVIEW.BCF background cached, then 4096 words overlayed as
  // 128x64 byte-pixels at (96,68), low byte left (VISUAL.C: video is short*).
  async function ensureMemBg(url) {
    const b = await getBcf(url);
    const img = offCtx.createImageData(320, 200);
    for (let i = 0; i < 320 * 200; i++) {
      const [r, g, bl] = b.pal[b.px[i]];
      img.data[i * 4] = r; img.data[i * 4 + 1] = g;
      img.data[i * 4 + 2] = bl; img.data[i * 4 + 3] = 255;
    }
    memBg = { pal: b.pal, img };
  }

  function blitMem(words) {
    ensureCtx();
    if (!gctx || !memBg) return;
    words = toBytes(words);
    const d = new Uint8ClampedArray(memBg.img.data); // copy background
    const pal = memBg.pal;
    for (let i = 0; i < 4096; i++) {
      const lo = words[2 * i], hi = words[2 * i + 1];
      const px = 96 + 2 * (i % 64), py = 68 + ((i / 64) | 0);
      let o = (py * 320 + px) * 4;
      let c = pal[lo];
      d[o] = c[0]; d[o + 1] = c[1]; d[o + 2] = c[2];
      o += 4;
      c = pal[hi];
      d[o] = c[0]; d[o + 1] = c[1]; d[o + 2] = c[2];
    }
    offCtx.putImageData(new ImageData(d, 320, 200), 0, 0);
    gctx.imageSmoothingEnabled = false;
    gctx.drawImage(off, 0, 0, W, H);
  }

  function beep() {
    try {
      audio = audio || new (window.AudioContext || window.webkitAudioContext)();
      const t = audio.currentTime;
      const osc = audio.createOscillator();
      const gain = audio.createGain();
      osc.type = "square";
      osc.frequency.value = 880;
      gain.gain.setValueAtTime(0.12, t);
      gain.gain.exponentialRampToValueAtTime(0.001, t + 0.09);
      osc.connect(gain).connect(audio.destination);
      osc.start(t);
      osc.stop(t + 0.1);
    } catch { /* no audio: silent like a muted speaker */ }
  }

  function saveFile(name, text) {
    const blob = new Blob([text], { type: "text/plain" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = name;
    a.click();
    setTimeout(() => URL.revokeObjectURL(a.href), 5000);
  }

  function focusKeys() {
    const el = document.getElementById("emuKeys");
    if (el) el.focus({ preventScroll: true });
  }

  const CMD = new Set(["Enter", "NumpadEnter", "Escape", "Space", "Tab",
    "ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight",
    "PageUp", "PageDown", "Backspace", "Home", "End", "Delete",
    "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"]);

  function attachKeys(dotnetRef) {
    keyRef = dotnetRef;
    keyEl = document.getElementById("emuKeys");
    if (!keyEl) return;
    keyEl.addEventListener("keydown", (e) => {
      if (!keyRef) return;
      let handled = false;
      if (e.altKey || CMD.has(e.code)) {
        handled = keyRef.invokeMethod("OnJsKey", e.code, e.key ?? "", e.altKey === true);
      } else if ((e.key ?? "").length === 1) {
        handled = keyRef.invokeMethod("OnJsChar", e.key);
      }
      if (handled) e.preventDefault();
    });
  }

  return { init, drawText, drawSplash, ensureMemBg, blitMem, beep, saveFile, focusKeys, attachKeys,
    _test: { getBcf, toBytes, PAL, COLS, ROWS } };
})();
