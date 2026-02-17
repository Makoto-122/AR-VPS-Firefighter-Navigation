# AR Firefighter Navigation using VPS

This project proposes an AR-based navigation system for firefighters using a Visual Positioning System (VPS).

## Background

In large and complex facilities, firefighters may lose time identifying fire sources and nearby water supply points. This study aims to reduce arrival time using AR navigation.

## System Overview

- Self-localization using Immersal VPS
- A* pathfinding algorithm
- Water-node prioritized routing
- AR overlay navigation in real-world space
- REST API (Flask) for simulated fire-source acquisition

## Experimental Result

In a controlled experiment with 45 participants:

- No navigation: 254.33 sec (avg)
- Floor plan: 218.13 sec (avg)
- AR navigation: 55.40 sec (avg)

→ Approx. 78% reduction compared to no-navigation condition.

## Technology Stack

- Unity 2021.3 LTS
- AR Foundation / ARKit
- Immersal SDK
- C#
- Flask (Python)

## Note

Immersal SDK is proprietary software by Immersal Ltd.
This repository contains research implementation using the SDK.
