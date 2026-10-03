# Changelog

All notable changes to the Roberts Space Industries (Star Citizen) Library plugin for Playnite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

---

## [0.1.0] - 2026-10-03

### Added
- **Channel & Installation Discovery**:
  - Zero-configuration detection of Star Citizen installations across all local drives and custom paths.
  - Full support for all release channels: `LIVE`, `PTU`, `EPTU`, `HOTFIX`, and `TECH-PREVIEW`.
  - Automatic SSD footprint calculation and semantic game versioning (e.g. `4.10.0`).
- **Telemetry & Pilot Identity**:
  - Automatic extraction of pilot callsign from local game logs.
  - Active shard, cluster, and environment detection.
- **Process Lifecycle & Launching**:
  - Native `AutomaticPlayController` routing launches via the RSI Launcher and actively monitors `StarCitizen.exe` process lifecycle in `Bin64`.
  - Single clean play action to eliminate redundant selection popups ("Aktion auswählen").
  - Safe metadata inheritance: persists playtime, categories, and custom tags across library updates.
- **UI, Localization & Theme Integration**:
  - Full bilingual support with English (`en_US.xaml`) default and German (`de_DE.xaml`) translations.
  - Explicit `RSI` source badge assignment for seamless integration with modern desktop themes (Penumbra) and plugins (DuplicateHider).
  - Playnite notification center feedback during library scan start, discovery, and completion.
- **CI/CD & Packaging**:
  - GitHub Actions automated build and `.pext` package verification (`build.yml`).
  - Automated release pipeline with SHA-256 checksums and release note generation (`release.yml`).
  - Added repository funding configuration and Ko-fi links (`https://ko-fi.com/goover`).
  - Local packaging script (`build.ps1`).
