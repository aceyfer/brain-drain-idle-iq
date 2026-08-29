# Project Overview
- Game Title: Brain Drain: Idle IQ
- High-Level Concept: Satirical mobile idle clicker by Eighth Kind Studios. Players tap to extract brain power, climb ranks, and buy upgrades.
- Players: Single player
- Inspiration / Reference Games: Idiocracy / Egg Inc / Adventure Capitalist
- Tone / Art Direction: Satirical cyberpunk 2D
- Target Platform: Android (Google Play) + iOS
- Screen Orientation / Resolution: Portrait 1080x1920
- Render Pipeline: URP 2D

# Game Mechanics
## Core Gameplay Loop
Update company name branding across all project metadata, plan files, and build configuration prior to Google Play release.

## Controls and Input Methods
N/A (Metadata & project configuration update)

# UI
N/A

# Key Asset & Context
- `ProjectSettings/ProjectSettings.asset`: Project PlayerSettings containing company name, product name, and Android application identifier.
- `Assets/Plans/*.md`: Plan documentation files containing historical company references.

# Implementation Steps
1. **Update Plan Documentation Files**
   - **Description**: Replace all historical occurrences of "AcEclipse Games" with "Eighth Kind Studios" across all files in `Assets/Plans/`.
   - **Assigned role**: developer
   - **Dependencies**: None
   - **Parallelizable**: Yes

2. **Verify and Update Unity PlayerSettings**
   - **Description**: Confirm `PlayerSettings.companyName` is set to "Eighth Kind Studios" and configure the Google Play Android package identifier (e.g. `com.eighthkindstudios.braindrain`).
   - **Assigned role**: developer
   - **Dependencies**: None
   - **Parallelizable**: Yes

# Verification & Testing
1. Search project repository with `Grep` to confirm 0 remaining references to "AcEclipse Games".
2. Read `ProjectSettings/ProjectSettings.asset` to verify `companyName` is "Eighth Kind Studios" and Android package identifier is valid for Google Play submission.
