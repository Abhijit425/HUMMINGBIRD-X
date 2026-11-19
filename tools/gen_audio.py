"""Synthesises every sound in HUMMINGBIRD X from scratch (original audio, no recordings).
Run: python3 tools/gen_audio.py -> Assets/HummingbirdX/Resources/Audio/*.wav"""
import os, wave
import numpy as np
from scipy import signal

SR = 32000
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "HummingbirdX", "Resources", "Audio")
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(7)


def t_(sec):
    return np.arange(int(sec * SR)) / SR


def bp(x, lo, hi, order=2):
    sos = signal.butter(order, [lo, hi], "bandpass", fs=SR, output="sos")
    return signal.sosfilt(sos, x)


def lp(x, f, order=2):
    return signal.sosfilt(signal.butter(order, f, "lowpass", fs=SR, output="sos"), x)


def hp(x, f, order=2):
    return signal.sosfilt(signal.butter(order, f, "highpass", fs=SR, output="sos"), x)


def pink(n):
    w = rng.standard_normal(n)
    f = np.fft.rfft(w)
    k = np.arange(len(f)); k[0] = 1
    return np.fft.irfft(f / np.sqrt(k), n)


def brown(n):
    b = np.cumsum(rng.standard_normal(n)); b = hp(b, 20)
    return b / (np.abs(b).max() + 1e-9)


def norm(x, peak=0.9):
    return x / (np.abs(x).max() + 1e-9) * peak


def env(n, a, r, sustain=None):
    e = np.ones(n)
    na, nr = int(a * SR), int(r * SR)
    na = min(na, n); nr = min(nr, n - na)
    e[:na] = np.linspace(0, 1, na) if na else e[:na]
    if nr: e[n - nr:] = np.linspace(1, 0, nr) ** 2
    return e


def loopify(x, fade=1.0):
    """x is generated (len+fade) long; crossfade the tail into the head."""
    nf = int(fade * SR)
    L = x.shape[0] - nf
    out = x[:L].copy()
    w = np.linspace(0, 1, nf)
    if x.ndim == 2: w = w[:, None]
    out[:nf] = x[:nf] * w + x[L:L + nf] * (1 - w)
    return out


def write(name, x):
    x = np.asarray(x, dtype=float)
    ch = 1 if x.ndim == 1 else x.shape[1]
    pcm = (np.clip(x, -1, 1) * 32767).astype("<i2")
    with wave.open(os.path.join(OUT, name), "wb") as w:
        w.setnchannels(ch); w.setsampwidth(2); w.setframerate(SR); w.writeframes(pcm.tobytes())
    print("wrote", name, f"{len(x) / SR:.1f}s", "stereo" if ch == 2 else "mono")


def place(buf, clip, at, gain=1.0, pan=0.0):
    i = int(at * SR)
    if i >= len(buf): return
    c = clip[: len(buf) - i] * gain
    if buf.ndim == 2:
        buf[i:i + len(c), 0] += c * np.sqrt(0.5 * (1 - pan))
        buf[i:i + len(c), 1] += c * np.sqrt(0.5 * (1 + pan))
    else:
        buf[i:i + len(c)] += c


# --------------------------------------------------------------- bird calls (one-shots)
def chirp(f0, f1, dur, harm=(1.0, 0.25, 0.08), vib=0.0, vibf=0.0, curve=1.0):
    t = t_(dur)
    u = (t / dur) ** curve
    f = f0 + (f1 - f0) * u + vib * np.sin(2 * np.pi * vibf * t)
    ph = 2 * np.pi * np.cumsum(f) / SR
    s = sum(a * np.sin(ph * (k + 1)) for k, a in enumerate(harm))
    return s * env(len(t), 0.01, dur * 0.4)


def call_whistle_descend():  # tinamou-like tremulous whistle
    parts = []
    for i in range(4):
        parts.append(chirp(1650 - i * 90, 1450 - i * 90, 0.42, (1, 0.05), vib=25, vibf=18, curve=0.7))
        parts.append(np.zeros(int(0.22 * SR)))
    return norm(np.concatenate(parts), 0.8)


def call_trill():  # wren-like rapid trill
    notes = []
    for i in range(22):
        f = 3800 + 700 * np.sin(i * 0.9)
        notes.append(chirp(f + 600, f - 500, 0.035, (1, 0.3, 0.1)))
        notes.append(np.zeros(int(0.012 * SR)))
    return norm(np.concatenate(notes), 0.7)


def call_two_note():
    a = chirp(2200, 3400, 0.18, (1, 0.1), curve=0.6)
    b = chirp(3300, 1900, 0.35, (1, 0.1), curve=1.4)
    return norm(np.concatenate([a, np.zeros(int(0.08 * SR)), b]), 0.75)


def call_parrot():  # harsh squawk: noisy FM with formants
    dur = 0.45; t = t_(dur)
    f = 900 + 500 * np.sin(2 * np.pi * 3 * t) + 150 * rng.standard_normal(len(t)).cumsum() / 200
    ph = 2 * np.pi * np.cumsum(f) / SR
    src = np.sign(np.sin(ph)) * 0.6 + np.sin(ph * 2) * 0.4
    src = src + 0.6 * bp(rng.standard_normal(len(t)), 1500, 4000)
    y = bp(src, 1100, 1500) * 1.2 + bp(src, 2400, 3200) + bp(src, 3800, 4600) * 0.5
    y *= env(len(t), 0.02, 0.15)
    sq = np.concatenate([y, np.zeros(int(0.15 * SR)), y[: int(0.3 * SR)] * env(int(0.3 * SR), 0.01, 0.1)])
    return norm(sq, 0.8)


def call_toucan():  # low croaking pulses
    out = []
    for i in range(7):
        d = 0.09; t = t_(d)
        pulse = np.sin(2 * np.pi * 420 * t) * np.exp(-t * 30) + 0.5 * np.sin(2 * np.pi * 840 * t) * np.exp(-t * 40)
        pulse += 0.4 * bp(rng.standard_normal(len(t)), 500, 1400) * np.exp(-t * 35)
        out += [pulse, np.zeros(int(0.07 * SR))]
    return norm(np.concatenate(out), 0.8)


def call_hb_chip():  # hummingbird chips
    out = []
    for i in range(5):
        out += [chirp(7200, 5600, 0.03, (1, 0.2)), np.zeros(int(rng.uniform(0.06, 0.14) * SR))]
    return norm(np.concatenate(out), 0.6)


def call_dove():  # soft coo (distant)
    c = []
    for f in (520, 560, 500):
        c += [chirp(f, f * 0.92, 0.32, (1, 0.35, 0.12), curve=1.2), np.zeros(int(0.12 * SR))]
    return norm(lp(np.concatenate(c), 1500), 0.7)


def call_oriole():  # liquid fluty phrase
    seq = [(1800, 2400, .12), (2400, 2100, .10), (2600, 3100, .14), (2000, 1600, .22)]
    c = []
    for a, b, d in seq:
        c += [chirp(a, b, d, (1, 0.12), vib=40, vibf=30), np.zeros(int(0.03 * SR))]
    return norm(np.concatenate(c), 0.75)


# --------------------------------------------------------------- loops
def amb_day(sec=40, fade=2):
    n = int((sec + fade) * SR)
    buf = np.zeros((n, 2))
    air = np.stack([hp(lp(pink(n), 2500), 80), hp(lp(pink(n), 2500), 80)], 1)
    buf += norm(air, 0.06)
    # leaf rustle swells
    sw = 0.5 + 0.5 * np.sin(2 * np.pi * np.cumsum(rng.uniform(0.02, 0.06, n)) / SR)
    rust = np.stack([bp(rng.standard_normal(n), 1800, 6000), bp(rng.standard_normal(n), 1800, 6000)], 1)
    buf += norm(rust, 0.035) * sw[:, None] ** 3
    # insect layers: buzzy AM tones
    t = np.arange(n) / SR
    for f, am, g in ((4200, 38, 0.025), (5600, 61, 0.018), (3300, 23, 0.015)):
        tone = np.sin(2 * np.pi * f * t) * (0.5 + 0.5 * np.sin(2 * np.pi * am * t)) ** 4
        swell = 0.6 + 0.4 * np.sin(2 * np.pi * t / rng.uniform(7, 13) + rng.uniform(0, 6))
        buf[:, 0] += tone * g * swell; buf[:, 1] += tone * g * (1.2 - swell * 0.4)
    # distant birds
    calls = [call_whistle_descend, call_trill, call_two_note, call_oriole, call_dove]
    for k in range(16):
        c = lp(calls[k % len(calls)](), 6000)
        place(buf, c, rng.uniform(0.5, sec - 3), gain=rng.uniform(0.05, 0.12), pan=rng.uniform(-0.9, 0.9))
    return loopify(norm(buf, 0.55), fade)


def amb_night(sec=40, fade=2):
    n = int((sec + fade) * SR)
    buf = np.zeros((n, 2))
    buf += norm(np.stack([lp(pink(n), 900), lp(pink(n), 900)], 1), 0.05)
    t = np.arange(n) / SR
    # crickets: pulsed chirps
    for f, rate, g, pan in ((4600, 3.2, 0.05, -0.6), (5100, 2.4, 0.04, 0.5), (4300, 4.1, 0.035, 0.1)):
        gate = (np.sin(2 * np.pi * rate * t + rng.uniform(0, 6)) > 0.55).astype(float)
        gate = lp(gate, 60)
        tone = np.sin(2 * np.pi * f * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 45 * t))
        place(buf, tone * gate, 0, g, pan)
    # tree frogs
    for k in range(40):
        d = 0.12; tt = t_(d)
        fr = np.sin(2 * np.pi * rng.uniform(1300, 2100) * tt) * (0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 90 * tt))) * env(len(tt), 0.005, 0.06)
        place(buf, fr, rng.uniform(0, sec), rng.uniform(0.03, 0.08), rng.uniform(-0.8, 0.8))
    return loopify(norm(buf, 0.5), fade)


def amb_rain(sec=24, fade=2):
    n = int((sec + fade) * SR)
    hiss = np.stack([bp(rng.standard_normal(n), 400, 9000), bp(rng.standard_normal(n), 400, 9000)], 1)
    body = np.stack([lp(pink(n), 1200), lp(pink(n), 1200)], 1)
    buf = norm(hiss, 0.3) + norm(body, 0.25)
    # drips on leaves
    for k in range(int(sec * 30)):
        d = 0.03; tt = t_(d)
        drip = np.sin(2 * np.pi * rng.uniform(900, 3000) * tt) * np.exp(-tt * 160)
        place(buf, drip, rng.uniform(0, sec + fade - 0.05), rng.uniform(0.05, 0.2), rng.uniform(-1, 1))
    return loopify(norm(buf, 0.6), fade)


def amb_wind(sec=24, fade=3):
    n = int((sec + fade) * SR)
    t = np.arange(n) / SR
    mod = 0.55 + 0.45 * np.sin(2 * np.pi * t / 9.0) * np.sin(2 * np.pi * t / 4.3 + 1)
    w = np.stack([bp(pink(n), 120, 900), bp(pink(n), 140, 1000)], 1) * mod[:, None]
    leaves = np.stack([bp(rng.standard_normal(n), 2500, 7000), bp(rng.standard_normal(n), 2500, 7000)], 1) * (mod[:, None] ** 3)
    return loopify(norm(norm(w, 0.6) + norm(leaves, 0.12), 0.6), fade)


def loop_waterfall(sec=12, fade=1.5):
    n = int((sec + fade) * SR)
    roar = norm(lp(brown(n), 700), 0.6) + norm(bp(pink(n), 300, 4000), 0.35) + norm(hp(rng.standard_normal(n), 4000), 0.06)
    return loopify(norm(roar, 0.8), fade)


def loop_river(sec=12, fade=1.5):
    n = int((sec + fade) * SR)
    base = norm(bp(pink(n), 200, 2500), 0.3)
    for k in range(int(sec * 18)):
        d = rng.uniform(0.04, 0.12); tt = t_(d)
        f0 = rng.uniform(250, 900)
        g = np.sin(2 * np.pi * np.cumsum(f0 * (1 + 1.5 * tt / d)) / SR) * np.exp(-tt * 30)
        place(base, g, rng.uniform(0, sec + fade - 0.2), rng.uniform(0.05, 0.18))
    return loopify(norm(base, 0.7), fade)


def loop_wing(sec=2.0):
    # 50 Hz wingbeat: harmonic hum + air turbulence modulated per stroke (exact loop: integer cycles)
    t = t_(sec)
    f = 50.0
    hum = sum((0.9 ** k) * np.sin(2 * np.pi * f * (k + 1) * t + k) for k in range(12))
    air = bp(rng.standard_normal(len(t)), 150, 1500) * (0.5 + 0.5 * np.sin(2 * np.pi * f * 2 * t)) ** 2
    y = norm(hum, 0.5) + norm(air, 0.35)
    y = hp(y, 60)
    # make the loop seamless by crossfading the edges a few ms
    nf = int(0.02 * SR); w = np.linspace(0, 1, nf)
    y[:nf] = y[:nf] * w + y[-nf:] * (1 - w)
    return norm(y, 0.8)


def loop_cicada(sec=8, fade=1):
    n = int((sec + fade) * SR); t = np.arange(n) / SR
    buzz = bp(rng.standard_normal(n), 4800, 7200) * (0.5 + 0.5 * np.sin(2 * np.pi * 180 * t)) ** 2
    swell = np.clip(np.sin(np.pi * t / (sec + fade)) * 1.4, 0, 1)
    return loopify(norm(buzz * (0.3 + 0.7 * swell), 0.6), fade)


# --------------------------------------------------------------- sfx
def thunder(seed):
    r = np.random.default_rng(seed)
    d = 6.0; n = int(d * SR); t = np.arange(n) / SR
    crack = hp(r.standard_normal(n), 300) * np.exp(-t * 9) * 0.4
    rumble = lp(np.cumsum(r.standard_normal(n)), 120)
    rumble = rumble / np.abs(rumble).max() * (1 - np.exp(-t * 6)) * np.exp(-t * 0.6)
    roll = 0.6 + 0.4 * np.abs(lp(r.standard_normal(n), 3)) / 0.2
    return norm(lp(crack, 2000) + rumble * np.clip(roll, 0, 1.5), 0.9)


def sip():
    out = np.zeros(int(0.5 * SR))
    for k in range(5):
        d = 0.05; tt = t_(d)
        s = bp(rng.standard_normal(len(tt)), 2500, 6000) * np.exp(-tt * 90) + 0.3 * np.sin(2 * np.pi * 1400 * tt) * np.exp(-tt * 70)
        place(out, s, k * 0.085, 0.8)
    return norm(out, 0.5)


def chime(freqs, step=0.09, decay=2.5, gain=0.6):
    d = step * len(freqs) + 1.4
    out = np.zeros(int(d * SR))
    for i, f in enumerate(freqs):
        tt = t_(1.3)
        tone = (np.sin(2 * np.pi * f * tt) + 0.3 * np.sin(2 * np.pi * f * 2.01 * tt) + 0.12 * np.sin(2 * np.pi * f * 3.98 * tt))
        place(out, tone * np.exp(-tt * decay) * env(len(tt), 0.004, 0.2), i * step, 0.5)
    return norm(out, gain)


def click(freq=1800, d=0.04, g=0.5):
    tt = t_(d)
    return norm(np.sin(2 * np.pi * freq * tt) * np.exp(-tt * 140) + 0.2 * hp(rng.standard_normal(len(tt)), 3000) * np.exp(-tt * 200), g)


def shutter():
    out = np.zeros(int(0.25 * SR))
    place(out, click(2600, 0.05, 1), 0.0); place(out, click(1700, 0.06, 0.8), 0.11)
    return norm(out, 0.7)


def whoosh():
    d = 0.7; n = int(d * SR); t = np.arange(n) / SR
    x = rng.standard_normal(n)
    sweep = np.zeros(n)
    for i in range(0, n, 1024):
        f = 300 + 2500 * (i / n)
        seg = bp(x[i:i + 1024 + 256], f, f * 2.2)[: min(1024, n - i)]
        sweep[i:i + len(seg)] = seg
    return norm(sweep * np.sin(np.pi * t / d) ** 2, 0.5)


if __name__ == "__main__":
    write("amb_forest_day.wav", amb_day())
    write("amb_forest_night.wav", amb_night())
    write("amb_rain.wav", amb_rain())
    write("amb_wind.wav", amb_wind())
    write("loop_waterfall.wav", loop_waterfall())
    write("loop_river.wav", loop_river())
    write("loop_wing.wav", loop_wing())
    write("loop_cicada.wav", loop_cicada())
    for i, fn in enumerate([call_whistle_descend, call_trill, call_two_note, call_oriole, call_dove, call_hb_chip]):
        write(f"sfx_bird_{i + 1}.wav", fn())
    write("sfx_parrot.wav", call_parrot())
    write("sfx_toucan.wav", call_toucan())
    write("sfx_thunder_1.wav", thunder(1)); write("sfx_thunder_2.wav", thunder(2))
    write("sfx_sip.wav", sip())
    write("sfx_reward.wav", chime([1046.5, 1318.5, 1568.0]))
    write("sfx_discover.wav", chime([784, 988, 1175, 1568, 1976], 0.12, 1.6, 0.7))
    write("sfx_tap.wav", click())
    write("sfx_shutter.wav", shutter())
    write("sfx_whoosh.wav", whoosh())
