# PRD: Synthos

## Summary

An API-first backend service that ingests audio files, analyzes them, and exposes the audio plus structured musical metadata (BPM, key, loudness, waveform data, etc.) via a REST API. Not a CRUD app — the value is in the analysis pipeline, streaming, and background processing.

**One-line pitch:** An API-first music intelligence service that turns audio files into structured musical data.

## Core Flow

```
Upload audio → store file + metadata → queue analysis job → background worker
extracts BPM/key/loudness/waveform → results stored → client polls/fetches results
→ client streams audio with seek support
```

## Architecture

- **API layer** — ASP.NET Core, handles upload, metadata, and serving results
- **Metadata DB** — PostgreSQL (track info, analysis results, job status) — not the audio bytes
- **File storage** — local disk in dev (`/storage/tracks/`), S3-compatible/R2 in production
- **Analysis worker** — background service (FFmpeg + DSP) that runs BPM/key/loudness/waveform extraction async, off the request thread
- **Job queue** — Redis (or similar) to coordinate upload → analysis → completion

## API Endpoints

```
POST   /tracks                        upload a track
GET    /tracks                        list tracks
GET    /tracks/{id}                   get track metadata
DELETE /tracks/{id}                   delete a track

GET    /tracks/{id}/analysis          analysis results (status: processing | complete)
GET    /tracks/{id}/waveform          downsampled waveform peaks
GET    /tracks/{id}/waveform?resolution=N   waveform at custom resolution

GET    /tracks/{id}/stream            audio stream (supports HTTP Range requests)

GET    /tracks/{id}/layers            list stems/layers (vocals, drums, bass, etc.)
GET    /tracks/{id}/layers/{name}/stream    stream a single layer
```

## Key Technical Pieces

**Async analysis, not blocking requests**
`POST /tracks` returns `202 Accepted` immediately; a background worker does the actual analysis. `GET /tracks/{id}/analysis` returns `{"status": "processing"}` until done, then the full result.

**Waveform downsampling**
Raw audio has millions of samples (~15.9M/channel for a 3-min stereo track at 44.1kHz). The API reduces this to a fixed number of peak values (e.g. 1000–10000 points) by taking max amplitude per chunk — a "waveform thumbnail" the frontend can render cheaply.

**Byte-range streaming**
`GET /tracks/{id}/stream` supports HTTP Range requests so clients can seek without downloading the full file — standard behavior for any real audio player.

**Layers/stems**
A track can have multiple associated audio layers (vocals, drums, bass, guitar) that share the same timeline, streamed independently, so a client can mute/solo channels.

## Sample Response Shapes

```json
// GET /tracks/{id}
{
  "id": "8f31...",
  "title": "My Song",
  "duration": 213.42,
  "format": "wav",
  "sampleRate": 44100,
  "channels": 2
}

// GET /tracks/{id}/analysis
{
  "status": "complete",
  "bpm": 127.4,
  "key": "C minor",
  "loudness": -11.4,
  "dynamicRange": 9.2
}

// GET /tracks/{id}/waveform
{
  "sampleRate": 44100,
  "resolution": 1000,
  "peaks": [0.12, 0.42, 0.31, 0.81, 0.67]
}

// GET /tracks/{id}/layers
[
  { "id": "1", "name": "Vocals" },
  { "id": "2", "name": "Drums" },
  { "id": "3", "name": "Bass" }
]
```

## Build Phases

| Level | Scope |
|---|---|
| 1 | Upload, store, list, delete, metadata |
| 2 | Core analysis: BPM, key, duration, sample rate, channels, loudness, dynamic range |
| 3 | Waveforms: raw + downsampled peaks, multiple resolutions, caching |
| 4 | Streaming: HTTP Range, seeking, partial responses, caching |
| 5 | Background processing: queue + worker pipeline |
| 6 | Layers/stems: master + vocals/drums/bass/guitar as separate streamable tracks |
| 7 | Music intelligence: beat positions, section labels (intro/verse/chorus), onsets, energy curves, similarity |

## Tech Stack

- ASP.NET Core (API)
- Entity Framework Core + PostgreSQL (data access, metadata)
- Background service / worker for analysis jobs
- Redis (job queue / caching)
- Docker Compose (api, postgres, worker, redis)
- FFmpeg + DSP libraries for audio analysis (not built from scratch)
- React frontend (waveform display, playback, layer mute/solo controls)

## Known Difficulty Ratings

- **Easy:** BPM detection, waveform extraction, loudness/dynamic-range calculation
- **Hard:** reliable musical key detection
- **Hardest (real DSP/ML territory):** stem separation, instrument recognition, chord recognition, section detection — use existing audio-processing libraries/tools rather than implementing the math from scratch

## Open Questions / Not Yet Decided

- Project name (candidates floated: Sonora, Resonance, Audiograph, Waveform)
- Which audio-analysis libraries/tools to integrate with for BPM/key/loudness
- Storage backend for production (S3 vs R2 vs other)
- Whether stem separation is in scope for v1 or a later phase